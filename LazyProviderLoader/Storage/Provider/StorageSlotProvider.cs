using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LazyProviderLoader.Storage.Provider
{
    public class StorageSlotProvider : IStorageSlotProvider
    {
        private readonly ILogger<StorageSlotProvider> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public StorageSlotProvider(ILogger<StorageSlotProvider> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public async Task<StorageSlot> GetAsync(int sessionId)
        {
            await Task.Delay(Constants.StorageCreationDelayMs);

            var storageLogger = _loggerFactory.CreateLogger<Storage>();

            var slot = new StorageSlot
            {
                // hardcoded invoices
                InvoiceIds = new HashSet<string> { "invoice1", "invoice2", "invoice3" },
                Storage = new Storage(sessionId, storageLogger)
            };

            _logger.LogInformation($"Slot created for session {sessionId}.");

            return slot;
        }
    }
}
