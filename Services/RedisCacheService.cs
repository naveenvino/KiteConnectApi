using StackExchange.Redis;
using System.Text.Json;

namespace KiteConnectApi.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _cache;

        public RedisCacheService(IDatabase cache)
        {
            _cache = cache;
        }

        public async Task<string?> GetStringAsync(string key)
        {
            return await _cache.StringGetAsync(key);
        }

        public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            await _cache.StringSetAsync(key, value, expiry);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _cache.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            await _cache.StringSetAsync(key, JsonSerializer.Serialize(value), expiry);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.KeyDeleteAsync(key);
        }
    }
}