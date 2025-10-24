using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using App.Application.UseCase.Rankings.WeeklyTopJumps;

namespace App.Infrastructure.ReadModels.Rankings.WeeklyTopJumps;

public class WeeklyTopJumpsCacheOptions
{
    /// <summary>Cache key (można modyfikować gdy trzeba wariantów)</summary>
    public string CacheKey { get; set; } = "weeklytopjumps:redis:top20:last7days";

    /// <summary>Domyślny czas trwania cache (absolute expiration).</summary>
    public TimeSpan AbsoluteExpirationRelativeToNow { get; set;  } = TimeSpan.FromMinutes(10);

    /// <summary>Max rozmiar elementu (opcjonalnie użyte przy MemoryCacheOptions sizing).</summary>
    public long? Size { get; set;  } = 1;
}

/// <summary>
/// Dekorator, który cachuje wyniki wywołania wewnętrznego IWeeklyTopJumpsQuery (np. Redis impl).
/// Zapobiega równoległym równoważnym zapytaniom (stampede) przez semafor per-key.
/// </summary>
public sealed class CachedWeeklyTopJumpsQuery(
    IWeeklyTopJumpsQuery inner,
    IMemoryCache cache,
    IOptions<WeeklyTopJumpsCacheOptions> optionsAccessor)
    : IWeeklyTopJumpsQuery
{
    private readonly WeeklyTopJumpsCacheOptions _options = optionsAccessor?.Value ?? new WeeklyTopJumpsCacheOptions();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<IReadOnlyList<WeeklyTopJumpDto>> GetTop20Last7Days(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var key = _options.CacheKey;

        if (cache.TryGetValue<IReadOnlyList<WeeklyTopJumpDto>>(key, out var cached))
        {
            return cached!;
        }

        var semaphoreSlim = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphoreSlim.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (cache.TryGetValue(key, out cached))
            {
                return cached!;
            }

            var result = await inner.GetTop20Last7Days(CancellationToken.None).ConfigureAwait(false);

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.AbsoluteExpirationRelativeToNow
            };

            if (_options.Size.HasValue)
                cacheEntryOptions.SetSize(_options.Size.Value);

            cache.Set(key, result, cacheEntryOptions);

            return result;
        }
        finally
        {
            semaphoreSlim.Release();
            _locks.TryRemove(key, out var maybeSem);
        }
    }
}