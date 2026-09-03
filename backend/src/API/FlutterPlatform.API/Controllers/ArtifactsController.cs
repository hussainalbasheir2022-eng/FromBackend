using FlutterPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlutterPlatform.API.Controllers;

[ApiController]
[Route("api/v1/artifacts")]
[AllowAnonymous]
public class ArtifactsController : ControllerBase
{
    private readonly IStorageService _storage;

    public ArtifactsController(IStorageService storage) => _storage = storage;

    [HttpGet("{**key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        key = Uri.UnescapeDataString(key);
        if (!await _storage.ExistsAsync(key, ct))
            return NotFound();

        var stream = await _storage.DownloadAsync(key, ct);
        return File(stream, "application/vnd.android.package-archive", "app-debug.apk");
    }
}
