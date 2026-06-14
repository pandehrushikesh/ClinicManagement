using System.Text.Json;
using ClinicManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace ClinicManagement.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var bytes = await _cache.GetAsync(key, cancellationToken);
        return bytes is null ? null : JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        await _cache.SetAsync(key, bytes, options, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(key, cancellationToken);

    // Redis doesn't have native prefix-delete without SCAN; we store a set of keys per prefix to track them.
    // For this phase, callers use specific keys — RemoveByPrefixAsync is a best-effort scan via the key tracker.
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var trackerKey = $"__keys__{prefix}";
        var tracked = await GetAsync<List<string>>(trackerKey, cancellationToken);
        if (tracked is null) return;

        foreach (var key in tracked)
            await _cache.RemoveAsync(key, cancellationToken);

        await _cache.RemoveAsync(trackerKey, cancellationToken);
    }
}
