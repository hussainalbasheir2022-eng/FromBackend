namespace FlutterPlatform.Application.Interfaces;

public interface IStorageService
{
    Task InitializeAsync();
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    string GetPublicUrl(string key);
}
