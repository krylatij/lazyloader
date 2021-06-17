

using System;

namespace LazyProviderLoader.Storage
{
    public interface IStorage : IDisposable
    {
        int GetSessionId();

        Guid GetStorageId();
    }
}
