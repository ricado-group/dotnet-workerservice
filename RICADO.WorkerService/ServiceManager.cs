using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using RICADO.Logging;

namespace RICADO.WorkerService
{
    public static class ServiceManager
    {
        #region Private Properties

        private static bool _initialized = false;

        private static IHostBuilder _hostBuilder;

        #endregion


        #region Public Properties

        #endregion


        #region Public Methods

        /// <summary>
        /// Initialize the Service (Host) Manager. This should be called at the very start of your Entry Point <c>Main</c> Method
        /// </summary>
        /// <param name="args">The <c>Main</c> Method Startup Arguments</param>
        public static void Initialize(string[] args)
        {
            if(_initialized == true)
            {
                return;
            }

            _hostBuilder = createHostBuilder(args);

            _initialized = true;
        }

        /// <summary>
        /// Adds a Hosted Service to be Started and Stopped by the Service (Host) Manager
        /// </summary>
        /// <typeparam name="TService">The Service Class</typeparam>
        /// <exception cref="System.InvalidOperationException">Thrown when AddService is called before Initialize</exception>
        public static void AddService<TService>() where TService : class, IHostedService
        {
            if(_initialized == false)
            {
                throw new InvalidOperationException("The *Initialize* Method must be called before attempting to Add a Service");
            }

            _hostBuilder.ConfigureServices(services => services.AddHostedService<TService>());
        }

        /// <summary>
        /// Runs the Service Host and Blocks the Calling Thread until a SIGTERM or Ctrl+C is received
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when Run is called before Initialize or when Run is called multiple times within a Service's Lifetime</exception>
        public static void Run()
        {
            if(_initialized == false)
            {
                throw new InvalidOperationException("The *Initialize* Method must be called before attempting to Run");
            }

            using IHost host = _hostBuilder.Build();

            try
            {
                host.Run();
            }
            catch (OperationCanceledException)
            {
            }
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Creates a Host Builder to Run Services for an Application
        /// </summary>
        /// <param name="args">The <c>Main</c> Method Startup Arguments</param>
        /// <returns>A Host Builder ready to call <c>.Build()</c></returns>
        private static IHostBuilder createHostBuilder(string[] args)
        {
            IHostBuilder builder = Host.CreateDefaultBuilder(args);

            // Logging

            builder.ConfigureLogging(loggerFactory =>
            {
                // Clear all Logging Providers
                loggerFactory.ClearProviders();
            });

            // Host Configuration

            builder.ConfigureHostConfiguration(config =>
            {
                try
                {
                    // Add an Optional hostsettings.json File
                    config.AddJsonFile(new PhysicalFileProvider("/conf"), "hostsettings.json", true, false);
                }
                catch
                {
                }
            });

            // Application Configuration

            builder.ConfigureAppConfiguration((hostContext, config) =>
            {
                IHostEnvironment env = hostContext.HostingEnvironment;

                try
                {
                    // Add an Optional appsettings.json File
                    config.AddJsonFile(new PhysicalFileProvider("/conf"), "appsettings.json", true, false);
                }
                catch
                {
                }

                try
                {
                    // Add an Optional appsettings.{Environment}.json File
                    config.AddJsonFile(new PhysicalFileProvider("/conf"), $"appsettings.{env.EnvironmentName}.json", true, false);
                }
                catch
                {
                }

                // Add the "RICADO_" Environment Variables
                config.AddEnvironmentVariables(prefix: "RICADO_");
            });

            // Services

            builder.ConfigureServices(services => services.AddHostedService<InternalWorker>());

            return builder;
        }

        #endregion
    }
}
