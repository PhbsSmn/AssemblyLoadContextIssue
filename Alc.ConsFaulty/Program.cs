using System;
using System.Linq;
using Alc.Services;
using Alc.Services.Root;
using Microsoft.Extensions.DependencyInjection;

namespace Alc.ConsFaulty
{
    class Program
    {
        #region Fields
        private static ServiceProvider ServiceProvider { get; set; }
        #endregion
        static void Main(string[] args)
        {
            ConfigureServicesStatic();
            try
            {
                var simpleServices = ServiceProvider.GetServices<ISimpleService>().ToList();
                foreach (var simpleService in simpleServices)
                {
                    Console.WriteLine(simpleService.LibraryVersion());
                }
                foreach (var simpleService in simpleServices)
                {
                    Console.WriteLine(simpleService.LibraryVersion());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ConfigureServicesStatic()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddTransient<ISimpleService, Services.ServerV2.SimpleService>();
            serviceCollection.AddTransient<ISimpleService, Services.ServerV1.SimpleService>();
            serviceCollection.AddSingleton<ISharedService, SharedService>();

            ServiceProvider = serviceCollection.BuildServiceProvider();

        }
    }
}
