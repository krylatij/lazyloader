using System;
using System.Threading.Tasks;
using LazyProviderLoader.Storage;
using Microsoft.Extensions.Logging;

namespace LazyProviderLoader.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IStorageFactory _storageFactory;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(IStorageFactory storageFactory, ILogger<InvoiceService> logger)
        {
            _storageFactory = storageFactory;
            _logger = logger;
        }

        public async Task ProcessAsync(int sessionId, string invoiceId)
        {
            var storage = await _storageFactory.GetStorageAsync(sessionId);

            try
            {
                _logger.LogInformation(
                    $"Got storage for session {sessionId} with storageId '{storage.GetStorageId()}' and invoice '{invoiceId}'.");

                await Task.Delay(Constants.InvoiceProcessingDelayMs);

                var storageSession = storage.GetSessionId();

                _logger.LogInformation($"Processed invoice '{invoiceId}' in session: '{storageSession}'.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during invoice processing.");
                throw;
            }
            finally
            {
                if (_storageFactory.TryToRemoveStorage(sessionId, invoiceId))
                {
                    _logger.LogInformation(
                        $"It was the last invoice {invoiceId} for session {sessionId}, storage disposal was triggered.");
                }
            }
        }
    }
}
