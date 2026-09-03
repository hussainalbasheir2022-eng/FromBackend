using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlutterPlatform.API.Controllers;

[ApiController]
[Route("api/v1/updates")]
[AllowAnonymous]
public class UpdatesController : ControllerBase
{
    private readonly IRepository<Release> _releases;
    private readonly IRepository<ReleaseManifest> _manifests;

    public UpdatesController(IRepository<Release> releases, IRepository<ReleaseManifest> manifests)
    {
        _releases = releases;
        _manifests = manifests;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest(
        [FromQuery] string applicationId,
        [FromQuery] string channel = "production",
        [FromQuery] int currentBuildNumber = 0,
        [FromQuery] string currentVersion = "0",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
            return BadRequest(new { error = "applicationId is required" });

        if (currentBuildNumber == 0 && int.TryParse(currentVersion, out var parsed))
            currentBuildNumber = parsed;

        // Flutter --split-per-abi encodes ABI into versionCode (arm64-v8a = 2000 + N).
        // The phone reports 2004 while our release BuildNumber is 4.
        currentBuildNumber = NormalizeFlutterBuildNumber(currentBuildNumber);

        var releases = await _releases.FindAsync(
            r => r.ApplicationId == applicationId
              && r.Channel == channel
              && r.Status == ReleaseStatus.Published, ct);

        var latest = releases
            .OrderByDescending(r => r.BuildNumber)
            .ThenByDescending(r => r.PublishedAt)
            .ToList();

        Release? chosen = null;
        ReleaseManifest? manifest = null;
        foreach (var candidate in latest)
        {
            if (candidate.BuildNumber <= currentBuildNumber)
                break;
            var found = (await _manifests.FindAsync(m => m.ReleaseId == candidate.Id, ct))
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.ArtifactUrl) && !string.IsNullOrWhiteSpace(m.Sha256));
            if (found == null)
                continue;
            chosen = candidate;
            manifest = found;
            break;
        }

        if (chosen == null || manifest == null)
            return Ok(new { available = false });

        return Ok(new
        {
            available = true,
            releaseId = chosen.Id,
            applicationId = chosen.ApplicationId,
            version = chosen.Version,
            buildNumber = chosen.BuildNumber,
            mandatory = chosen.IsMandatory,
            minimumVersion = chosen.MinimumVersion,
            releaseNotes = chosen.ReleaseNotes,
            manifest = manifest == null ? null : new
            {
                artifactUrl = manifest.ArtifactUrl,
                sha256 = manifest.Sha256,
                signature = manifest.Signature,
                createdAt = manifest.CreatedAt
            }
        });
    }

    internal static int NormalizeFlutterBuildNumber(int versionCode)
    {
        if (versionCode < 1000)
            return versionCode;
        return versionCode % 1000;
    }
}
