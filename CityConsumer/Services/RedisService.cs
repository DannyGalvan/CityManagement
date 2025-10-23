using CityConsumer.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace CityConsumer.Services
{

    public class RedisService : IRedisService
    {
        private readonly IDatabase _db;
        private readonly ILogger<RedisService> _logger;

        public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
        {
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                return await _db.StringSetAsync(key, value, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al establecer clave en Redis: {key}");
                return false;
            }
        }

        public async Task<string?> GetStringAsync(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                return value.HasValue ? value.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener clave de Redis: {key}");
                return null;
            }
        }

        public async Task<bool> DeleteAsync(string key)
        {
            try
            {
                return await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar clave de Redis: {key}");
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar existencia en Redis: {key}");
                return false;
            }
        }

        public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                return await _db.StringSetAsync(key, json, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al serializar/guardar objeto en Redis: {key}");
                return false;
            }
        }

        public async Task<T?> GetObjectAsync<T>(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                if (!value.HasValue) return default;

                return JsonSerializer.Deserialize<T>(value.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener/deserializar objeto de Redis: {key}");
                return default;
            }
        }

        // Nuevos métodos útiles para correlación

        public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null)
        {
            try
            {
                var result = await _db.StringIncrementAsync(key, value);
                if (expiry.HasValue && result == value) // Solo establecer expiry si es nuevo
                {
                    await _db.KeyExpireAsync(key, expiry.Value);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al incrementar clave en Redis: {key}");
                return -1;
            }
        }

        public async Task<bool> ExpireAsync(string key, TimeSpan expiry)
        {
            try
            {
                return await _db.KeyExpireAsync(key, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al establecer expiración en Redis: {key}");
                return false;
            }
        }

        public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
        {
            try
            {
                return await _db.KeyTimeToLiveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener TTL de Redis: {key}");
                return null;
            }
        }

        // Métodos para trabajar con conjuntos (útil para correlaciones)

        public async Task<bool> SetAddAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                var result = await _db.SetAddAsync(key, value);
                if (expiry.HasValue && result)
                {
                    await _db.KeyExpireAsync(key, expiry.Value);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al agregar a conjunto en Redis: {key}");
                return false;
            }
        }

        public async Task<string[]> SetMembersAsync(string key)
        {
            try
            {
                var values = await _db.SetMembersAsync(key);
                return values.Select(v => v.ToString()).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener miembros del conjunto en Redis: {key}");
                return Array.Empty<string>();
            }
        }

        public async Task<long> SetLengthAsync(string key)
        {
            try
            {
                return await _db.SetLengthAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener tamaño del conjunto en Redis: {key}");
                return 0;
            }
        }

        // Método para operaciones en lote (más eficiente)

        public async Task<bool> SetMultipleAsync(Dictionary<string, string> keyValues, TimeSpan? expiry = null)
        {
            try
            {
                var batch = _db.CreateBatch();
                var tasks = new List<Task>();

                foreach (var kvp in keyValues)
                {
                    tasks.Add(batch.StringSetAsync(kvp.Key, kvp.Value, expiry));
                }

                batch.Execute();
                await Task.WhenAll(tasks);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al establecer múltiples claves en Redis");
                return false;
            }
        }

        public async Task<Dictionary<string, string?>> GetMultipleAsync(params string[] keys)
        {
            try
            {
                var values = await _db.StringGetAsync(keys.Select(k => (RedisKey)k).ToArray());
                var result = new Dictionary<string, string?>();

                for (int i = 0; i < keys.Length; i++)
                {
                    result[keys[i]] = values[i].HasValue ? values[i].ToString() : null;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener múltiples claves de Redis");
                return new Dictionary<string, string?>();
            }
        }

        // Método para búsqueda de patrones (usar con cuidado en producción)

        public async Task<List<string>> SearchKeysAsync(string pattern, int maxKeys = 100)
        {
            try
            {
                var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern, pageSize: maxKeys).Take(maxKeys);
                return keys.Select(k => k.ToString()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al buscar claves en Redis con patrón: {pattern}");
                return new List<string>();
            }
        }

        public async Task<long> DeletePatternAsync(string pattern)
        {
            try
            {
                var keys = await SearchKeysAsync(pattern, 1000);
                if (keys.Count == 0) return 0;

                return await _db.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar claves con patrón: {pattern}");
                return 0;
            }
        }
    }
}
