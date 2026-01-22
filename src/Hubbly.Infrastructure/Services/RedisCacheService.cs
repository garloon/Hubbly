using Hubbly.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Hubbly.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var cachedData = await _cache.GetStringAsync(key);
        return string.IsNullOrEmpty(cachedData)
            ? default
            : JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new DistributedCacheEntryOptions();

        if (expiry.HasValue)
        {
            options.SetAbsoluteExpiration(expiry.Value);
        }
        else
        {
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
        }

        var serializedData = JsonSerializer.Serialize(value, _jsonOptions);
        await _cache.SetStringAsync(key, serializedData, options);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var cachedData = await _cache.GetAsync(key);
        return cachedData != null;
    }
}