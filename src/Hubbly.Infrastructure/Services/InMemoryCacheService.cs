using Hubbly.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Hubbly.Infrastructure.Services;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

    public InMemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        var value = _memoryCache.Get<T>(key);
        Console.WriteLine($"InMemoryCache Get: Key={key}, Value={value}"); // Добавьте логирование
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        Console.WriteLine($"InMemoryCache Set: Key={key}, Value={value}, Expiry={expiry}"); // Добавьте логирование

        var options = new MemoryCacheEntryOptions();
        options.SetAbsoluteExpiration(expiry ?? TimeSpan.FromMinutes(10));

        _memoryCache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        var exists = _memoryCache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }
}