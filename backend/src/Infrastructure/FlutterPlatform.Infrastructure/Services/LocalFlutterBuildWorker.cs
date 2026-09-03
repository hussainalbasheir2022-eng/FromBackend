using System.Diagnostics;
using System.Security.Cryptography;
using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using FlutterPlatform.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlutterPlatform.Infrastructure.Services;

public class LocalFlutterBuildWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IBuildQueue _queue;
    private readonly IConfiguration _config;
    private readonly ILogger<LocalFlutterBuildWorker> _logger;

    public LocalFlutterBuildWorker(
        IServiceScopeFactory scopes,
        IBuildQueue queue,
        IConfiguration config,
        ILogger<LocalFlutterBuildWorker> logger)
    {
        _scopes = scopes;
        _queue = queue;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OTA build worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var buildId = await _queue.DequeueAsync(stoppingToken);
                if (buildId == null)
                {
                    await Task.Delay(1500, stoppingToken);
                    continue;
                }

                await ProcessAsync(buildId.Value, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Build worker loop failed");
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(Guid buildId, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var builds = scope.ServiceProvider.GetRequiredService<IRepository<Build>>();
        var files = scope.ServiceProvider.GetRequiredService<IProjectFileRepository>();
        var projects = scope.ServiceProvider.GetRequiredService<IRepository<Project>>();
        var releases = scope.ServiceProvider.GetRequiredService<IRepository<Release>>();
        var manifests = scope.ServiceProvider.GetRequiredService<IRepository<ReleaseManifest>>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var notifier = scope.ServiceProvider.GetRequiredService<ISignalRNotifier>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var build = await builds.GetByIdAsync(buildId, ct);
        if (build == null) return;

        var project = await projects.GetByIdAsync(build.ProjectId, ct);
        if (project == null)
        {
            build.Status = BuildStatus.Failed;
            build.ErrorMessage = "Project not found";
            await builds.UpdateAsync(build, ct);
            return;
        }

        build.Status = BuildStatus.Running;
        build.StartedAt = DateTime.UtcNow;
        await builds.UpdateAsync(build, ct);
        await notifier.NotifyBuildStarted(buildId, project.Name);
        await LogAsync(db, buildId, "Preparing workspace...", ct);

        var root = _config["Flutter:ProjectRoot"] ?? @"D:\FromBackend\flutter-preview";
        var flutter = _config["Flutter:SdkPath"] ?? "flutter";
        var publicBase = (_config["Flutter:PublicBaseUrl"] ?? "http://192.168.1.110:5194").TrimEnd('/');
        Directory.CreateDirectory(root);

        try
        {
            var projectFiles = await files.GetByProjectAsync(project.Id, ct);
            foreach (var file in projectFiles)
            {
                if (file.Path.StartsWith("android/", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (file.Path.StartsWith("lib/update/", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (file.Path.Equals("lib/main.dart", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (file.Path.Equals("pubspec.yaml", StringComparison.OrdinalIgnoreCase))
                    continue;

                var full = Path.Combine(root, file.Path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                await File.WriteAllTextAsync(full, file.Content ?? "", ct);
            }

            await WriteBootstrapAsync(root, publicBase, project.ApplicationId, ct);
            await PatchPubspecVersionAsync(root, project.Version, build.BuildNumber, ct);
            await PatchAndroidApplicationIdAsync(root, project.ApplicationId, ct);

            await LogAsync(db, buildId, "flutter clean", ct);
            await RunAsync(flutter, "clean", root, ct, async line =>
            {
                await notifier.NotifyBuildLog(buildId, line);
            });

            await LogAsync(db, buildId, "flutter pub get", ct);
            var pubGet = await RunAsync(flutter, "pub get", root, ct, async line =>
            {
                await notifier.NotifyBuildLog(buildId, line);
            });
            await FlushProcessLogsAsync(db, buildId, pubGet.Lines, ct);
            if (pubGet.ExitCode != 0)
                throw new Exception("flutter pub get failed");

            await LogAsync(db, buildId, $"Building APK {project.Version}+{build.BuildNumber}...", ct);
            await notifier.NotifyBuildLog(buildId, "Building ARM64 release APK with Flutter SDK...");

            var exit = await RunAsync(flutter,
                "build apk --release --split-per-abi --target-platform android-arm64",
                root, ct, async line =>
            {
                await notifier.NotifyBuildLog(buildId, line);
            });
            await FlushProcessLogsAsync(db, buildId, exit.Lines, ct);

            if (exit.ExitCode != 0)
                throw new Exception("flutter build apk failed");

            var apkPath = FindBuiltApk(root);
            if (apkPath == null)
                throw new Exception("APK not found after build");

            await using var stream = File.OpenRead(apkPath);
            var sha = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct)).ToLowerInvariant();
            stream.Position = 0;

            var key = $"builds/{buildId}/{Path.GetFileName(apkPath)}";
            await storage.UploadAsync(key, stream, "application/vnd.android.package-archive", ct);
            var artifactUrl = $"{publicBase}/api/v1/artifacts/{key}";

            build.Status = BuildStatus.Succeeded;
            build.CompletedAt = DateTime.UtcNow;
            build.Sha256 = sha;
            build.ArtifactSize = new FileInfo(apkPath).Length;
            build.ArtifactUrl = artifactUrl;
            await builds.UpdateAsync(build, ct);

            if (PublishIntentStore.TryTake(buildId, out var intent) && intent != null)
            {
                var release = new Release
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    BuildId = build.Id,
                    ApplicationId = project.ApplicationId,
                    Version = $"{project.Version}+{build.BuildNumber}",
                    BuildNumber = build.BuildNumber,
                    Channel = intent.Channel,
                    Status = ReleaseStatus.Published,
                    IsMandatory = intent.Mandatory,
                    ReleaseNotes = intent.ReleaseNotes,
                    PublishedAt = DateTime.UtcNow,
                    RolloutPercentage = 100,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await releases.AddAsync(release, ct);

                var manifest = new ReleaseManifest
                {
                    Id = Guid.NewGuid(),
                    ReleaseId = release.Id,
                    ApplicationId = release.ApplicationId,
                    Version = release.Version,
                    BuildNumber = release.BuildNumber,
                    ArtifactUrl = artifactUrl,
                    Sha256 = sha,
                    Signature = sha,
                    GeneratedAt = DateTime.UtcNow,
                    IsMandatory = intent.Mandatory,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await manifests.AddAsync(manifest, ct);

                await LogAsync(db, buildId, $"Release published v{release.Version}", ct);
                await notifier.NotifyReleasePublished(release.Id, release.ApplicationId, release.Version, intent.Mandatory);
            }

            await LogAsync(db, buildId, "BUILD SUCCEEDED", ct);
            await notifier.NotifyBuildCompleted(buildId, true, artifactUrl);
        }
        catch (Exception ex)
        {
            try
            {
                db.ChangeTracker.Clear();
                var failed = await builds.GetByIdAsync(buildId, ct);
                if (failed != null)
                {
                    failed.Status = BuildStatus.Failed;
                    failed.ErrorMessage = ex.Message;
                    failed.CompletedAt = DateTime.UtcNow;
                    await builds.UpdateAsync(failed, ct);
                }
                await LogAsync(db, buildId, $"BUILD FAILED: {ex.Message}", ct);
            }
            catch (Exception inner)
            {
                _logger.LogError(inner, "Failed to persist build failure for {BuildId}", buildId);
            }
            await notifier.NotifyBuildFailed(buildId, ex.Message);
        }
    }

    private static string? FindBuiltApk(string root)
    {
        var dir = Path.Combine(root, "build", "app", "outputs", "flutter-apk");
        string[] candidates =
        [
            "app-arm64-v8a-release.apk",
            "app-armeabi-v7a-release.apk",
            "app-release.apk",
            "app-arm64-v8a-debug.apk",
            "app-debug.apk"
        ];
        foreach (var name in candidates)
        {
            var path = Path.Combine(dir, name);
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    private static async Task WriteBootstrapAsync(string root, string publicBase, string applicationId, CancellationToken ct)
    {
        var updateDir = Path.Combine(root, "lib", "update");
        Directory.CreateDirectory(updateDir);
        await File.WriteAllTextAsync(Path.Combine(updateDir, "config.dart"),
            "const List<String> kPlatformBaseUrls = [\n" +
            $"  '{publicBase}',\n" +
            "  'http://127.0.0.1:5194',\n" +
            "];\n" +
            $"const String kApplicationId = '{applicationId}';\n" +
            "const String kUpdateChannel = 'production';\n", ct);

        await File.WriteAllTextAsync(Path.Combine(root, "lib", "main.dart"), """
import 'package:flutter/material.dart';
import 'app.dart';
import 'update/update_agent.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  UpdateAgent.instance.start();
  runApp(const MyApp());
}
""", ct);
    }

    private static async Task PatchAndroidApplicationIdAsync(string root, string applicationId, CancellationToken ct)
    {
        var gradle = Path.Combine(root, "android", "app", "build.gradle.kts");
        if (!File.Exists(gradle)) return;
        var text = await File.ReadAllTextAsync(gradle, ct);
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"applicationId\s*=\s*""[^""]+""",
            $"applicationId = \"{applicationId}\"");
        await File.WriteAllTextAsync(gradle, text, ct);
    }

    private static async Task PatchPubspecVersionAsync(string root, string versionName, int versionCode, CancellationToken ct)
    {
        var pubspec = Path.Combine(root, "pubspec.yaml");
        if (!File.Exists(pubspec)) return;
        var text = await File.ReadAllTextAsync(pubspec, ct);
        text = System.Text.RegularExpressions.Regex.Replace(
            text, @"^version:\s*.+$", $"version: {versionName}+{versionCode}",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        await File.WriteAllTextAsync(pubspec, text, ct);
    }

    private static async Task LogAsync(AppDbContext db, Guid buildId, string message, CancellationToken ct)
    {
        db.BuildLogs.Add(MakeLog(buildId, message));
        await db.SaveChangesAsync(ct);
    }

    private static async Task FlushProcessLogsAsync(AppDbContext db, Guid buildId, IReadOnlyList<string> lines, CancellationToken ct)
    {
        if (lines.Count == 0) return;
        foreach (var line in lines)
            db.BuildLogs.Add(MakeLog(buildId, line));
        await db.SaveChangesAsync(ct);
    }

    private static BuildLog MakeLog(Guid buildId, string message) => new()
    {
        Id = Guid.NewGuid(),
        BuildId = buildId,
        Message = message.Length > 4000 ? message[..4000] : message,
        Level = "Information",
        Timestamp = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static async Task<(int ExitCode, IReadOnlyList<string> Lines)> RunAsync(
        string fileName, string args, string workDir, CancellationToken ct, Func<string, Task> onLine)
    {
        var sdk = Environment.GetEnvironmentVariable("ANDROID_HOME")
            ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk");

        var isBat = fileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase);
        var psi = new ProcessStartInfo
        {
            FileName = isBat ? "cmd.exe" : fileName,
            Arguments = isBat ? $"/c \"\"{fileName}\" {args}\"" : args,
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["ANDROID_HOME"] = sdk;
        psi.Environment["ANDROID_SDK_ROOT"] = sdk;

        var lines = new System.Collections.Concurrent.ConcurrentQueue<string>();
        using var p = new Process { StartInfo = psi };
        p.Start();
        await Task.WhenAll(
            ReadAsync(p.StandardOutput, onLine, lines, ct),
            ReadAsync(p.StandardError, onLine, lines, ct));
        await p.WaitForExitAsync(ct);
        return (p.ExitCode, lines.ToArray());
    }

    private static async Task ReadAsync(
        StreamReader reader, Func<string, Task> onLine,
        System.Collections.Concurrent.ConcurrentQueue<string> lines, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            lines.Enqueue(line);
            try { await onLine(line); } catch { /* live SignalR must not fail the build */ }
        }
    }
}
