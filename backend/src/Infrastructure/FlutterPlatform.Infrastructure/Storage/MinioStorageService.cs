using FlutterPlatform.Application.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace FlutterPlatform.Infrastructure.Storage;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucket;
    private readonly string _publicEndpoint;

    public MinioStorageService(IMinioClient client, string bucket, string publicEndpoint = "")
    {
        _client = client;
        _bucket = bucket;
        _publicEndpoint = publicEndpoint;
    }

    public async Task InitializeAsync()
    {
        var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
        if (!exists)
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType), ct);
        return GetPublicUrl(key);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key)
            .WithCallbackStream(s => s.CopyTo(ms)), ct);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
        => await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_bucket).WithObject(key), ct);

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _client.StatObjectAsync(new StatObjectArgs().WithBucket(_bucket).WithObject(key), ct);
            return true;
        }
        catch { return false; }
    }

    public string GetPublicUrl(string key)
        => string.IsNullOrEmpty(_publicEndpoint)
            ? $"/api/v1/artifacts/{Uri.EscapeDataString(key)}"
            : $"{_publicEndpoint}/{_bucket}/{key}";
}
