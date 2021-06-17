using System.Collections.Generic;

namespace LazyProviderLoader.Storage
{
    public class StorageSlot
    {
        public IStorage Storage { get; set; }

        public HashSet<string> InvoiceIds { get; set; }
    }
}
