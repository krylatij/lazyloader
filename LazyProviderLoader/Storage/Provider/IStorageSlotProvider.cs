// Copyright (c) ThomsonReuters. All rights reserved.

using System.Threading.Tasks;

namespace LazyProviderLoader.Storage.Provider
{
    public interface IStorageSlotProvider
    {
        Task<StorageSlot> GetAsync(int sessionId);
    }
}
