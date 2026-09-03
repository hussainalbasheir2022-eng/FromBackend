using FlutterPlatform.Application.Interfaces;

namespace FlutterPlatform.Infrastructure.Storage;

public class LocalFileStorageService : IStorageService
{
    private readonly string _root;

    public LocalFileStorageService(string root)
    {
        _root = root;
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        return Task.CompletedTask;
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var path = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var fs = File.Create(path);
        await content.CopyToAsync(fs, ct);
        return GetPublicUrl(key);
    }

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var path = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var path = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult(File.Exists(path));
    }

    public string GetPublicUrl(string key)
        => $"/api/v1/artifacts/{Uri.EscapeDataString(key)}";
}
