using FlutterPlatform.Application.Commands.Builds;
using FlutterPlatform.Application.Commands.Files;
using FlutterPlatform.Application.Commands.Releases;
using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Application.Queries.Projects;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlutterPlatform.API.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Project> _projects;
    private readonly IRepository<Build> _builds;
    private readonly IStorageService _storage;
    private readonly ILocalDeviceDeployer _deviceDeployer;

    public ProjectsController(
        IMediator mediator,
        IRepository<Project> projects,
        IRepository<Build> builds,
        IStorageService storage,
        ILocalDeviceDeployer deviceDeployer)
    {
        _mediator = mediator;
        _projects = projects;
        _builds = builds;
        _storage = storage;
        _deviceDeployer = deviceDeployer;
    }

    private Guid CurrentUserId => CurrentUser.GetId(User);

    /// <summary>List all projects</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectsQuery(), ct);
        return Ok(result);
    }

    /// <summary>Get project by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id), ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new Flutter project</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest req, CancellationToken ct)
    {
        var applicationId = string.IsNullOrWhiteSpace(req.ApplicationId)
            ? $"com.flutterplatform.app_{Guid.NewGuid():N}"
            : req.ApplicationId.Trim();

        var existing = await _projects.FindAsync(p => p.ApplicationId == applicationId, ct);
        if (existing.Count > 0)
            return BadRequest(new { error = "This package name is already bound to another project. Each APK must have a unique Application ID." });

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Description = req.Description ?? "",
            ApplicationId = applicationId,
            DisplayName = req.DisplayName ?? req.Name,
            Version = req.Version ?? "1.0.0",
            BuildNumber = 1,
            OwnerId = CurrentUserId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _projects.AddAsync(project, ct);

        // Create default Flutter project files
        await SeedDefaultFlutterFiles(project.Id, ct);

        return Created($"/api/v1/projects/{project.Id}", new { id = project.Id });
    }

    /// <summary>Update project metadata</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest req, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(id, ct);
        if (project == null) return NotFound();

        if (req.Name != null) project.Name = req.Name;
        if (req.Description != null) project.Description = req.Description;
        if (req.DisplayName != null) project.DisplayName = req.DisplayName;
        if (req.Version != null) project.Version = req.Version;
        project.UpdatedAt = DateTime.UtcNow;

        await _projects.UpdateAsync(project, ct);
        return NoContent();
    }

    /// <summary>Delete a project</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(id, ct);
        if (project == null) return NotFound();
        await _projects.DeleteAsync(project, ct);
        return NoContent();
    }

    /// <summary>Download the latest successful APK for this project (first-install / share).</summary>
    [HttpGet("{id:guid}/apk")]
    public async Task<IActionResult> DownloadApk(Guid id, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(id, ct);
        if (project == null) return NotFound();

        var builds = await _builds.FindAsync(b => b.ProjectId == id && b.Status == BuildStatus.Succeeded, ct);
        var latest = builds.OrderByDescending(b => b.BuildNumber).FirstOrDefault();
        if (latest == null)
            return NotFound(new { error = "No APK yet. Click Publish to build the first APK for this project." });

        var key = ArtifactStorageKey(latest.ArtifactUrl, latest.Id);
        if (!await _storage.ExistsAsync(key, ct))
        {
            foreach (var fallback in new[]
            {
                $"builds/{latest.Id}/app-arm64-v8a-release.apk",
                $"builds/{latest.Id}/app-release.apk",
                $"builds/{latest.Id}/app-debug.apk"
            })
            {
                if (await _storage.ExistsAsync(fallback, ct))
                {
                    key = fallback;
                    break;
                }
            }
        }
        if (!await _storage.ExistsAsync(key, ct))
            return NotFound(new { error = "APK file is missing. Publish again." });

        var stream = await _storage.DownloadAsync(key, ct);
        var name = $"{project.ApplicationId}-v{latest.BuildNumber}.apk";
        return File(stream, "application/vnd.android.package-archive", name);
    }

    /// <summary>Trigger a build for this project</summary>
    [HttpPost("{id:guid}/build")]
    public async Task<IActionResult> Build(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBuildCommand(id, CurrentUserId), ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Accepted(new { buildId = result.BuildId });
    }

    /// <summary>
    /// Export project sources to the local Flutter workspace, build APK, and install on the USB-connected phone.
    /// </summary>
    [HttpPost("{id:guid}/run-device")]
    public async Task<IActionResult> RunOnDevice(Guid id, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(id, ct);
        if (project == null) return NotFound();

        var result = await _deviceDeployer.DeployAsync(id, ct);
        if (!result.Success)
            return BadRequest(new { error = result.Error, log = result.Log });
        return Ok(new { success = true, log = result.Log });
    }

    /// <summary>Publish: queue a real Flutter APK build and notify devices when it succeeds.</summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishProjectRequest? req, CancellationToken ct)
    {
        var result = await _mediator.Send(new PublishProjectCommand(
            id,
            CurrentUserId,
            req?.Channel ?? "production",
            req?.Mandatory ?? true,
            req?.ReleaseNotes), ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Accepted(new { buildId = result.BuildId });
    }

    /// <summary>Analyze the project (queues analyze build)</summary>
    [HttpPost("{id:guid}/analyze")]
    public async Task<IActionResult> Analyze(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBuildCommand(id, CurrentUserId, "analyze"), ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Accepted(new { buildId = result.BuildId });
    }

    private static string ArtifactStorageKey(string? artifactUrl, Guid buildId)
    {
        const string marker = "/api/v1/artifacts/";
        if (!string.IsNullOrWhiteSpace(artifactUrl))
        {
            var i = artifactUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (i >= 0)
                return Uri.UnescapeDataString(artifactUrl[(i + marker.Length)..]);
        }
        return $"builds/{buildId}/app-arm64-v8a-release.apk";
    }

    private async Task SeedDefaultFlutterFiles(Guid projectId, CancellationToken ct)
    {
        var files = DefaultFlutterTemplate.GetFiles();
        foreach (var (path, content) in files)
            await _mediator.Send(new UpsertProjectFileCommand(projectId, path, content, CurrentUserId), ct);
    }
}

public record PublishProjectRequest(string? Channel, bool? Mandatory, string? ReleaseNotes);

public record CreateProjectRequest(
    string Name,
    string ApplicationId,
    string? Description,
    string? DisplayName,
    string? Version
);

public record UpdateProjectRequest(
    string? Name,
    string? Description,
    string? DisplayName,
    string? Version
);

public static class DefaultFlutterTemplate
{
    public static Dictionary<string, string> GetFiles() => new()
    {
        ["lib/main.dart"] = """
import 'package:flutter/material.dart';
import 'app.dart';

void main() {
  runApp(const MyApp());
}
""",
        ["lib/app.dart"] = """
import 'package:flutter/material.dart';

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Flutter Platform App',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
        useMaterial3: true,
      ),
      home: const HomePage(),
    );
  }
}

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Flutter Platform')),
      body: const Center(
        child: Text('Version 1', style: TextStyle(fontSize: 24)),
      ),
    );
  }
}
""",
        ["pubspec.yaml"] = """
name: flutter_platform_app
description: A Flutter app managed by Flutter Platform.

publish_to: 'none'

version: 1.0.0+1

environment:
  sdk: '>=3.0.0 <4.0.0'

dependencies:
  flutter:
    sdk: flutter
  http: ^1.2.0
  shared_preferences: ^2.2.0
  package_info_plus: ^5.0.0

dev_dependencies:
  flutter_test:
    sdk: flutter
  flutter_lints: ^3.0.0

flutter:
  uses-material-design: true
""",
        ["README.md"] = """
# Flutter Platform App

This project is managed by the Flutter Platform Cloud IDE.

## Development

Edit files in the web IDE, then click **Build** or **Publish**.
"""
    };
}
