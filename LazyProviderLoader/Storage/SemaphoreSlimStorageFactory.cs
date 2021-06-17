using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LazyProviderLoader.Storage.Provider;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LazyProviderLoader.Storage
{
    public class SemaphoreSlimStorageFactory : IStorageFactory
    {
        private readonly IStorageSlotProvider _storageSlotProvider;
        private readonly IMemoryCache _cache;

        private readonly ConcurrentDictionary<int, SemaphoreSlim> _waiters = new ConcurrentDictionary<int, SemaphoreSlim>();
        
        private readonly ILogger<SemaphoreSlimStorageFactory> _logger;

        public SemaphoreSlimStorageFactory(IStorageSlotProvider storageSlotProvider, 
            IMemoryCache cache, 
            ILogger<SemaphoreSlimStorageFactory> logger)
        {
            _storageSlotProvider = storageSlotProvider;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IStorage> GetStorageAsync(int sessionId)
        {
            var key = GetCacheKey(sessionId);

            // trying to get pre-created storage
            if (_cache.TryGetValue(key, out StorageSlot entry))
            {
                return entry.Storage;
            }

            var waiter = _waiters.GetOrAdd(sessionId, (k) => new SemaphoreSlim(1, 1));

            await waiter.WaitAsync();

            try
            {
                // trying to get if already storage
                if (_cache.TryGetValue(key, out entry))
                {
                    return entry.Storage;
                }

                await InitStorageSlot(sessionId);

                if (_cache.TryGetValue(key, out entry))
                {
                    return entry.Storage;
                }

                // it is possible to get there in case of exception during storage initialization,
                // so all the waiting calls will fail
                // you how to handle it additionally, e.g. re-run initialization using while lo
                throw new InvalidOperationException($"Failed to initialize storage for session {sessionId}.");
            }
            finally
            {
                waiter.Release();
            }
        }

        private async Task InitStorageSlot(int sessionId)
        {
            _logger.LogInformation($"Initialization of a storage for the session '{sessionId}' is started.");

            try
            {
                var slot = await _storageSlotProvider.GetAsync(sessionId);

                var entryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMilliseconds(Constants.StorageExpirationMs))
                    .RegisterPostEvictionCallback((expiredKey, value, reason, state) =>
                    {
                        if (!(value is StorageSlot expiredSlot))
                        {
                            _logger.LogWarning($"strange data for key {expiredKey}");
                            return;
                        }

                        DisposeSlotSafe(expiredSlot);

                        _logger.LogInformation($"Storage was removed from cache with following reason '{reason}' and disposed.");
                    });

                var key = GetCacheKey(sessionId);
                _cache.Set(key, slot, entryOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during storage initialization for session {sessionId}");
            }
            finally
            {
                _waiters.TryRemove(sessionId, out _);
                _logger.LogInformation($"Remove storage initialization task for session {sessionId}.");
            }
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

            if (!_cache.TryGetValue(key, out StorageSlot slot))
            {
                _logger.LogInformation($"Storage not found in cache by key: {key}");
                return false;
            }

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
