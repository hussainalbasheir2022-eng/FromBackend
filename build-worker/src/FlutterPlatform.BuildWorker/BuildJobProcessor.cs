using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using StackExchange.Redis;

namespace FlutterPlatform.BuildWorker;

public class BuildJobProcessor
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _config;
    private readonly ILogger<BuildJobProcessor> _logger;
    private readonly IMinioClient _minio;
    private readonly string _bucket;
    private const string QueueKey = "build:queue";

    public BuildJobProcessor(
        IConnectionMultiplexer redis,
        IConfiguration config,
        ILogger<BuildJobProcessor> logger,
        IMinioClient minio)
    {
        _redis = redis;
        _config = config;
        _logger = logger;
        _minio = minio;
        _bucket = config["MinIO:BucketName"] ?? "flutter-platform";
    }

    private DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder()
            .UseSqlServer(_config.GetConnectionString("DefaultConnection"))
            .Options;
        return new BuildDbContext(options);
    }

    public async Task<Guid?> DequeueAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var val = await db.ListLeftPopAsync(QueueKey);
        if (val.IsNullOrEmpty) return null;
        return Guid.TryParse(val.ToString(), out var id) ? id : null;
    }

    public async Task ProcessAsync(Guid buildId, CancellationToken ct)
    {
        _logger.LogInformation("Processing build {BuildId}", buildId);

        await using var dbCtx = (BuildDbContext)CreateDbContext();

        var build = await dbCtx.Builds
            .Include(b => b.ProjectVersion)
            .ThenInclude(pv => pv!.Project)
            .FirstOrDefaultAsync(b => b.Id == buildId, ct);

        if (build == null)
        {
            _logger.LogWarning("Build {BuildId} not found", buildId);
            return;
        }

        build.Status = "Running";
        build.StartedAt = DateTime.UtcNow;
        await dbCtx.SaveChangesAsync(ct);

        var workDir = Path.Combine(Path.GetTempPath(), "flutter-builds", buildId.ToString());

        try
        {
            Directory.CreateDirectory(workDir);

            // Write project files to workspace
            await WriteProjectFilesAsync(dbCtx, build.ProjectId, workDir, ct);
            await LogAsync(dbCtx, buildId, "Workspace prepared", ct);

            // Determine Flutter SDK path
            var flutterPath = _config["Flutter:SdkPath"] ?? "/flutter/bin/flutter";

            // Run flutter pub get
            await LogAsync(dbCtx, buildId, "Running flutter pub get...", ct);
            var pubGetResult = await RunCommandAsync(flutterPath, "pub get", workDir, ct,
                async line => await LogAsync(dbCtx, buildId, line, ct));

            if (pubGetResult != 0)
                throw new Exception("flutter pub get failed");

            // Run flutter analyze
            await LogAsync(dbCtx, buildId, "Running flutter analyze...", ct);
            await RunCommandAsync(flutterPath, "analyze --no-fatal-warnings", workDir, ct,
                async line => await LogAsync(dbCtx, buildId, line, ct));

            // Run flutter build apk
            await LogAsync(dbCtx, buildId, "Building APK (release)...", ct);
            var buildResult = await RunCommandAsync(flutterPath,
                "build apk --release --split-per-abi --target-platform android-arm,android-arm64 --no-tree-shake-icons", workDir, ct,
                async line => await LogAsync(dbCtx, buildId, line, ct));

            if (buildResult != 0)
                throw new Exception("flutter build apk failed");

            // Prefer the arm64 split APK; fall back to a fat release APK.
            var apkDir = Path.Combine(workDir, "build", "app", "outputs", "flutter-apk");
            var apkPath = Path.Combine(apkDir, "app-arm64-v8a-release.apk");
            if (!File.Exists(apkPath))
                apkPath = Path.Combine(apkDir, "app-release.apk");
            if (!File.Exists(apkPath))
                throw new Exception($"APK not found at {apkDir}");

            await LogAsync(dbCtx, buildId, $"APK built: {new FileInfo(apkPath).Length / 1024} KB", ct);

            // Sign APK (jarsigner)
            var keystore = _config["Signing:KeystorePath"];
            var keystorePass = _config["Signing:KeystorePassword"];
            var keyAlias = _config["Signing:KeyAlias"];
            var keyPass = _config["Signing:KeyPassword"];

            if (!string.IsNullOrEmpty(keystore) && File.Exists(keystore))
            {
                await LogAsync(dbCtx, buildId, "Signing APK...", ct);
                var jarsigner = _config["Signing:JarsignerPath"] ?? "jarsigner";
                var signResult = await RunCommandAsync(jarsigner,
                    $"-verbose -keystore \"{keystore}\" -storepass \"{keystorePass}\" -keypass \"{keyPass}\" \"{apkPath}\" {keyAlias}",
                    workDir, ct, async line => await LogAsync(dbCtx, buildId, line, ct));

                if (signResult != 0)
                    _logger.LogWarning("APK signing returned non-zero exit for build {BuildId}", buildId);
                else
                    await LogAsync(dbCtx, buildId, "APK signed successfully", ct);
            }
            else
            {
                await LogAsync(dbCtx, buildId, "⚠️  No signing key configured - APK is unsigned (dev only)", ct);
            }

            // Compute SHA-256
            var sha256 = await ComputeSha256Async(apkPath);
            await LogAsync(dbCtx, buildId, $"SHA-256: {sha256}", ct);

            // Upload to MinIO
            await LogAsync(dbCtx, buildId, "Uploading artifact...", ct);
            var artifactKey = $"builds/{buildId}/app-release.apk";
            await using var apkStream = File.OpenRead(apkPath);
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(artifactKey)
                .WithStreamData(apkStream)
                .WithObjectSize(new FileInfo(apkPath).Length)
                .WithContentType("application/vnd.android.package-archive"), ct);

            var artifactUrl = $"{_config["MinIO:PublicEndpoint"]}/{_bucket}/{artifactKey}";

            // Update build record
            build.Status = "Succeeded";
            build.CompletedAt = DateTime.UtcNow;
            build.ArtifactUrl = artifactUrl;
            build.Sha256 = sha256;
            build.ArtifactSize = new FileInfo(apkPath).Length;
            build.FlutterSdkVersion = await GetFlutterVersionAsync(flutterPath);
            await dbCtx.SaveChangesAsync(ct);

            await LogAsync(dbCtx, buildId, $"✅ BUILD SUCCEEDED — {artifactUrl}", ct);
            _logger.LogInformation("Build {BuildId} succeeded", buildId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build {BuildId} failed", buildId);
            build.Status = "Failed";
            build.ErrorMessage = ex.Message;
            build.CompletedAt = DateTime.UtcNow;
            await dbCtx.SaveChangesAsync(ct);
            await LogAsync(dbCtx, buildId, $"❌ BUILD FAILED: {ex.Message}", ct);
        }
        finally
        {
            // Cleanup workspace
            try { Directory.Delete(workDir, true); }
            catch { /* best effort */ }
        }
    }

    private static async Task WriteProjectFilesAsync(BuildDbContext ctx, Guid projectId, string workDir, CancellationToken ct)
    {
        var files = await ctx.ProjectFiles
            .Where(f => f.ProjectId == projectId)
            .ToListAsync(ct);

        foreach (var file in files)
        {
            var fullPath = Path.Combine(workDir, file.Path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, file.Content ?? "", ct);
        }
    }

    private static async Task<int> RunCommandAsync(
        string executable, string arguments, string workDir,
        CancellationToken ct, Func<string, Task>? onOutput = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var tasks = new List<Task>
        {
            ReadStreamAsync(process.StandardOutput, onOutput, ct),
            ReadStreamAsync(process.StandardError, onOutput, ct)
        };

        await Task.WhenAll(tasks);
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }

    private static async Task ReadStreamAsync(
        System.IO.StreamReader reader, Func<string, Task>? handler, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            if (handler != null) await handler(line);
        }
    }

    private static async Task LogAsync(BuildDbContext ctx, Guid buildId, string message, CancellationToken ct)
    {
        ctx.BuildLogs.Add(new BuildLogEntry
        {
            Id = Guid.NewGuid(),
            BuildId = buildId,
            Message = message,
            Level = message.StartsWith("❌") ? "error" : "info",
            Timestamp = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync(ct);
    }

    private static async Task<string> ComputeSha256Async(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.Create().ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLower();
    }

    private static async Task<string?> GetFlutterVersionAsync(string flutterPath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = flutterPath, Arguments = "--version",
                RedirectStandardOutput = true, UseShellExecute = false
            };
            using var p = Process.Start(psi)!;
            var output = await p.StandardOutput.ReadToEndAsync();
            await p.WaitForExitAsync();
            var match = System.Text.RegularExpressions.Regex.Match(output, @"Flutter (\S+)");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch { return null; }
    }
}
