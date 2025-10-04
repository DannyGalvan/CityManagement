using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityConsumer.Interfaces
{
    public interface IRedisService
    {
        Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetStringAsync(string key);
        Task<bool> DeleteAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<T?> GetObjectAsync<T>(string key);
    }
}
