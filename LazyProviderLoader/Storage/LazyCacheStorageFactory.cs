using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using LazyProviderLoader.Storage.Provider;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LazyProviderLoader.Storage
{
    public class LazyCacheStorageFactory : IStorageFactory
    {
        private readonly IStorageSlotProvider _storageSlotProvider;
        private readonly IAppCache _cache;
     
        private readonly ILogger<WaitersStorageFactory> _logger;

        public LazyCacheStorageFactory(
            IStorageSlotProvider storageSlotProvider, 
            IAppCache cache,
            ILogger<WaitersStorageFactory> logger)
        {
            _storageSlotProvider = storageSlotProvider;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IStorage> GetStorageAsync(int sessionId)
        {
            var key = GetCacheKey(sessionId);

            var entry = await _cache.GetOrAddAsync(key, async (x) =>
            {
                x.SlidingExpiration = TimeSpan.FromMilliseconds(Constants.StorageExpirationMs);
                x.RegisterPostEvictionCallback((expiredKey, value, reason, state) =>
                {
                    if (!(value is Task<StorageSlot> expiredSlot))
                    {
                        _logger.LogWarning($"Strange data for key {expiredKey}");
                        return;
                    }

                    DisposeSlotSafe(expiredSlot.Result);

                    _logger.LogInformation($"Storage was removed from cache with following reason '{reason}' and disposed.");
                });

                return await _storageSlotProvider.GetAsync(sessionId);
            });

            if (entry == null)
            {
                throw new InvalidOperationException($"Failed to get storage from cache for session {sessionId}");
            }

            return entry.Storage;
        }

        private static string GetCacheKey(int sessionId)
        {
            return $"Storage4Session_{sessionId}";
        }

        private void DisposeSlotSafe(StorageSlot slot)
        {
            try
            {
                slot.Storage?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Exception during storage disposal.");
            }
        }

        public bool TryToRemoveStorage(int sessionId, string invoiceId)
        {
            var key = GetCacheKey(sessionId);

            if (!_cache.TryGetValue<StorageSlot>(key, out var rawSlot))
            {
                _logger.LogInformation($"Storage not found in cache by key: {key}");
                return false;
            }

            var lazy = (AsyncLazy<StorageSlot>) rawSlot;

            if (!lazy.IsValueCreated)
            {
                throw new InvalidOperationException($"Storage for session {sessionId} is not created, but trying to remove it.");
            }

            var slot = ((AsyncLazy<StorageSlot>)rawSlot).Value.Result;

            slot.InvoiceIds.RemoveWhere(x => x == invoiceId);
            _logger.LogInformation($"Invoice with id '{invoiceId}' is removed from storage for session '{sessionId}'");

            var ids = slot.InvoiceIds.Where(x => x != null).ToArray();
            if (ids.Length > 0)
            {
                var idsString = string.Join(", ", ids);
                
                _logger.LogInformation($"Storage is not ready for disposal. Waiting for invoices: '{idsString}' to complete");
                return false;
            }

            //!!! you can add basic locking here in case you need 100% guarantee Dispose will not be calls multiple times.
            
            _logger.LogInformation("No one invoice to wait. Let a disposing begin.");
            _cache.Remove(key);
            DisposeSlotSafe(slot);

            return true;
        }
    }
}
