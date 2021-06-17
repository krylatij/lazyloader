using System.Threading.Tasks;

namespace LazyProviderLoader.Storage.Factory
{
    public interface IStorageFactory
    {
        Task<IStorage> GetStorageAsync(int sessionId);

        bool TryToRemoveStorage(int sessionId, string invoiceId);
    }
}
