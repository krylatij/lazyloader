using System;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using LazyCache;
using LazyProviderLoader.Services;
using LazyProviderLoader.Storage;
using LazyProviderLoader.Storage.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LazyProviderLoader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var services = new ServiceCollection();
                
                services.AddMemoryCache();
                services.AddLogging(x => x.AddConsole(o =>
                {
                    o.DisableColors = false;
                    o.IncludeScopes = false;
                    o.TimestampFormat = "hh:mm:ss ";
                }));
           //     services.AddSingleton<IStorageFactory, WaitersStorageFactory>();
                services.AddSingleton<IAppCache, CachingService>();

                services.AddSingleton<IStorageFactory, LazyCacheStorageFactory>();
                services.AddSingleton<IStorageSlotProvider, StorageSlotProvider>();
                services.AddScoped<IInvoiceService, InvoiceService>();

                var provider = services.BuildServiceProvider();
                
                var t1 = Task.Run(async () =>
                {
                    var scope = provider.CreateScope();
                    var service = scope.ServiceProvider.GetService<IInvoiceService>();

                    await service.ProcessAsync(1, "invoice1");
                });

                var t2 = Task.Run(async () =>
                {
                    var scope = provider.CreateScope();
                    var service = scope.ServiceProvider.GetService<IInvoiceService>();

                    await service.ProcessAsync(1, "invoice2");
                });

                var t3 = Task.Run(async () =>
                {
                    var scope = provider.CreateScope();
                    var service = scope.ServiceProvider.GetService<IInvoiceService>();

                    await service.ProcessAsync(1, "invoice3");
                });


                await Task.WhenAll(t1, t2, t3);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.ReadLine();
            }
        }
    }
}
