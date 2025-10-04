using CityConsumer.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityConsumer.Services
{
    
    public class RedisService(IConnectionMultiplexer redis) : IRedisService
    {
        private readonly IDatabase _db = redis.GetDatabase();

        public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            return await _db.StringSetAsync(key, value, expiry);
        }

        public async Task<string?> GetStringAsync(string key)
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<bool> DeleteAsync(string key)
        {
            return await _db.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }

        public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            return await _db.StringSetAsync(key, json, expiry);
        }

        public async Task<T?> GetObjectAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue) return default;

            return System.Text.Json.JsonSerializer.Deserialize<T>(value.ToString());
        }
    }
}
