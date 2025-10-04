using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace PortalAcademico.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task RemoveByPrefixAsync(string prefix);
    }

    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var data = await _cache.GetStringAsync(key);
                
                if (string.IsNullOrEmpty(data))
                {
                    _logger.LogDebug("Cache MISS para key: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Cache HIT para key: {Key}", key);
                return JsonSerializer.Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener del cache key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions();
                
                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration;
                }

                var data = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, data, options);
                
                _logger.LogDebug("Datos guardados en cache con key: {Key}, expira en: {Expiration}s", 
                    key, expiration?.TotalSeconds ?? -1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar en cache key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Cache invalidado para key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al invalidar cache key: {Key}", key);
            }
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            // Nota: Redis no soporta eliminar por prefijo nativamente desde IDistributedCache
            // Para producción, considera usar StackExchange.Redis directamente
            _logger.LogWarning("RemoveByPrefixAsync no implementado completamente. Usa keys específicas.");
            await Task.CompletedTask;
        }
    }
}