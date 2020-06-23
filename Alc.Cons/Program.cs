using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using Alc.Services;
using Alc.Services.Root;
using Microsoft.Extensions.DependencyInjection;

namespace Alc.Cons
{
    class Program
    {
        #region Fields
        public static bool IsConfiguringServices { get; private set; }
        private static ServiceProvider ServiceProvider { get; set; }
        #endregion

        static void Main(string[] args)
        {
            ConfigureServicesDynamic();
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



        #region Helpers
        private static List<ServiceLoadContext> loadedContexts = new List<ServiceLoadContext>();

        private static void ConfigureServicesDynamic()
        {
            try
            {
                IsConfiguringServices = true;

                // Retrieve the netcore version & create the netcore app directory name where the matching modules are located (.netcore is not supposed to be backward compatible like full .net)
                var framework = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
                var frameworkDirectory = "netcoreapp" + framework.Split(',')[1].Split('=')[1].Replace("v", string.Empty);

                // These are the services that I'm willing to load dy
                var dynamicLoadableServices = new List<Type> { typeof(ISimpleService) };

                var serviceCollection = new ServiceCollection();


                var moduleDirectories = Directory.GetDirectories(@"..\Modules"); ;
                foreach (var moduleDirectory in moduleDirectories)
                {
                    // Retrieve the dll's from the same netcore directory & iterate only the ones that are passing the csharp assembly test.
                    var potentialCSharpAssemblies = Directory.GetFiles(Path.Combine(moduleDirectory, frameworkDirectory), "*.dll", SearchOption.TopDirectoryOnly);
                    foreach (var validCSharpAssemblyRelativePath in potentialCSharpAssemblies.Where(IsValidAssembly))
                    {
                        // All the path where relative up to now but contexts need a full path.
                        var validCSharpAssemblyFullPath = new FileInfo(validCSharpAssemblyRelativePath).FullName;

                        // Load the assembly in a contextual way.
                        var assemblyInContext = new ServiceLoadContext(validCSharpAssemblyFullPath);
                        var assemblyLoadedInContext = assemblyInContext.LoadFromAssemblyPath(validCSharpAssemblyFullPath);

                        // Where only loading classes, so dismiss all the rest immediatly
                        foreach (var definedContextClass in assemblyLoadedInContext.DefinedTypes.Where(type => type.IsClass))
                        {
                            // Load the services we want to load dynamically
                            foreach (var dynamicLoadableService in dynamicLoadableServices)
                            {
                                // Iterate the loaded class for the implemented interfaces
                                foreach (var definedContextClassInterface in definedContextClass.ImplementedInterfaces)
                                {
                                    // If the GUID & fullname of the interface is the same we will most likely deal with the same interface.
                                    if (dynamicLoadableService.FullName == definedContextClassInterface.FullName &&
                                        dynamicLoadableService.GUID == definedContextClassInterface.GUID)
                                    {
                                        // Add the class to the IoC container as a type.
                                        serviceCollection.AddTransient(dynamicLoadableService, definedContextClass);

                                        // Verify if the context is loaded in a global variable that's available during the life time of the serviceprovider.
                                        // Otherwise the load of the class will throw errors with dependent references.
                                        if (!loadedContexts.Contains(assemblyInContext))
                                        {
                                            loadedContexts.Add(assemblyInContext);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                serviceCollection.AddSingleton<ISharedService, SharedService>();

                ServiceProvider = serviceCollection.BuildServiceProvider();
            }
            finally
            {
                IsConfiguringServices = false;
            }
        }

        private static bool IsValidAssembly(string assemblyPath)
        {
            try
            {
                AssemblyName.GetAssemblyName(assemblyPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }

    public class ServiceLoadContext : AssemblyLoadContext
    {
        #region Fields
        private AssemblyDependencyResolver _resolver;
        #endregion

        #region Constructors
        public ServiceLoadContext(string mainAssemblyToLoadPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }
        #endregion

        #region Methods
        protected override Assembly Load(AssemblyName name)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(name);
            if (!Program.IsConfiguringServices && assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
        #endregion
    }
}
