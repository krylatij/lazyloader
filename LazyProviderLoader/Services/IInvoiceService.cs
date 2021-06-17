

using System.Threading.Tasks;

namespace LazyProviderLoader.Services
{
    public interface IInvoiceService
    {
        Task ProcessAsync(int sessionId, string invoiceId);
    }
}
