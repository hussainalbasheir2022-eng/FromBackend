using System.Diagnostics;
using System.Text;
using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlutterPlatform.Infrastructure.Services;

public class LocalDeviceDeployer : ILocalDeviceDeployer
{
    private readonly IProjectFileRepository _files;
    private readonly IRepository<Project> _projects;
    private readonly IConfiguration _config;
    private readonly ILogger<LocalDeviceDeployer> _logger;

    public LocalDeviceDeployer(
        IProjectFileRepository files,
        IRepository<Project> projects,
        IConfiguration config,
        ILogger<LocalDeviceDeployer> logger)
    {
        _files = files;
        _projects = projects;
        _config = config;
        _logger = logger;
    }

    public async Task<LocalDeployResult> DeployAsync(Guid projectId, CancellationToken ct = default)
    {
        var log = new StringBuilder();
        void Line(string msg)
        {
            log.AppendLine(msg);
            _logger.LogInformation("{Message}", msg);
        }

        try
        {
            var root = _config["Flutter:ProjectRoot"]
                ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "flutter-preview"));
            root = Path.GetFullPath(root);
            Directory.CreateDirectory(root);

            Line($"Writing project files to {root}");
            var files = await _files.GetByProjectAsync(projectId, ct);
            if (files.Count == 0)
                return new LocalDeployResult(false, log.ToString(), "Project has no files");

            foreach (var file in files)
            {
                var full = Path.Combine(root, file.Path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                await File.WriteAllTextAsync(full, file.Content ?? "", ct);
                Line($"  wrote {file.Path}");
            }

            var flutter = _config["Flutter:SdkPath"] ?? "flutter";
            var adb = _config["Flutter:AdbPath"]
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Android", "Sdk", "platform-tools", "adb.exe");

            Line("Building ARM64 release APK...");
            var build = await RunAsync(flutter,
                "build apk --release --split-per-abi --target-platform android-arm64",
                root, ct, Line);
            if (build != 0)
                return new LocalDeployResult(false, log.ToString(), "flutter build apk failed");

            var apkDir = Path.Combine(root, "build", "app", "outputs", "flutter-apk");
            var apk = Path.Combine(apkDir, "app-arm64-v8a-release.apk");
            if (!File.Exists(apk))
                apk = Path.Combine(apkDir, "app-release.apk");
            if (!File.Exists(apk))
                return new LocalDeployResult(false, log.ToString(), "APK not found after build");

            Line("Installing on device...");
            var install = await RunAsync(adb, $"install -r \"{apk}\"", root, ct, Line);
            if (install != 0)
                return new LocalDeployResult(false, log.ToString(),
                    "Install failed. On Xiaomi enable USB debugging + Install via USB, then tap Allow on the phone.");

            Line("Launching app...");
            var project = await _projects.GetByIdAsync(projectId, ct);
            var appId = project?.ApplicationId ?? "com.example.flutter_preview";
            await RunAsync(adb, $"shell am start -n {appId}/com.example.flutter_preview.MainActivity", root, ct, Line);

            Line("Deployed to phone.");
            return new LocalDeployResult(true, log.ToString(), null);
        }
        catch (Exception ex)
        {
            Line(ex.ToString());
            return new LocalDeployResult(false, log.ToString(), ex.Message);
        }
    }

    private static async Task<int> RunAsync(
        string fileName, string args, string workDir, CancellationToken ct, Action<string> onLine)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };
        p.OutputDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        await p.WaitForExitAsync(ct);
        return p.ExitCode;
    }
}
