using System;
using Microsoft.Extensions.Logging;

namespace LazyProviderLoader.Storage
{
    public class Storage : IStorage
    {
        private readonly int _sessionId;
        private readonly ILogger<Storage> _logger;
        private readonly Guid _storageId;

        private bool _disposed = false;

        public Storage(int sessionId, ILogger<Storage> logger)
        {
            _sessionId = sessionId;
            _logger = logger;

            _storageId = Guid.NewGuid();
        }

        public int GetSessionId()
        {
            return _sessionId;
        }

        public Guid GetStorageId()
        {
            return _storageId;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _logger.LogInformation($"Disposing storage {_storageId} for {_sessionId}.");
        }
    }
}
