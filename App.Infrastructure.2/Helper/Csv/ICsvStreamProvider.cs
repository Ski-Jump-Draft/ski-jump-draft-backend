using Amazon.S3;
using Amazon.S3.Model;
using App.Infrastructure._2.Repository.GameWorld.Country;
using App.Infrastructure._2.Repository.GameWorld.Hill;
using App.Infrastructure._2.Repository.GameWorld.Jumper;
using Microsoft.Extensions.Caching.Memory;

namespace App.Infrastructure._2.Helper.Csv;

public interface ICsvStreamProvider
{
    Task<Stream> Open(CancellationToken ct);
}

public class FileCsvStreamProvider(string path) : IGameWorldJumpersCsvStreamProvider,
    IGameWorldCountriesCsvStreamProvider, IGameWorldHillsCsvStreamProvider
{
    public Task<Stream> Open(CancellationToken ct) => Task.FromResult<Stream>(File.OpenRead(path));
}

public class S3CsvStreamProvider(IAmazonS3 s3, string bucket, string key) : IGameWorldJumpersCsvStreamProvider,
    IGameWorldCountriesCsvStreamProvider, IGameWorldHillsCsvStreamProvider
{
    public async Task<Stream> Open(CancellationToken ct)
    {
        var req = new GetObjectRequest { BucketName = bucket, Key = key };
        var resp = await s3.GetObjectAsync(req, ct);
        // nie zamykaj response streamu tutaj — skopiuj do MemoryStream żeby było bezpiecznie
        var ms = new MemoryStream();
        await resp.ResponseStream.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }
}

public class HttpCsvStreamProvider(HttpClient client, string url) : IGameWorldJumpersCsvStreamProvider,
    IGameWorldCountriesCsvStreamProvider, IGameWorldHillsCsvStreamProvider
{
    public async Task<Stream> Open(CancellationToken ct)
    {
        var s = await client.GetStreamAsync(url, ct);
        var ms = new MemoryStream();
        await s.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }
}

public class CachingCsvStreamProvider(ICsvStreamProvider inner, IMemoryCache cache, string cacheKey, TimeSpan ttl)
    : IGameWorldJumpersCsvStreamProvider, IGameWorldCountriesCsvStreamProvider,
        IGameWorldHillsCsvStreamProvider
{
    public async Task<Stream> Open(CancellationToken ct)
    {
        if (cache.TryGetValue(cacheKey, out byte[]? cachedBytes))
        {
            if (cachedBytes != null) return new MemoryStream(cachedBytes, writable: false);
        }

        await using var fresh = await inner.Open(ct);
        using var ms = new MemoryStream();
        await fresh.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        cache.Set(cacheKey, bytes, ttl);

        return new MemoryStream(bytes, writable: false);
    }
}