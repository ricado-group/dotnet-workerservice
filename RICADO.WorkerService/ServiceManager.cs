using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RICADO.Logging;

namespace RICADO.WorkerService
{
    public static class ServiceManager
    {
        #region Private Properties

        private static bool _initialized = false;
        private static object _initializedLock = new object();

        private static bool _shutdown = false;
        private static object _shutdownLock = new object();

        private static IHostBuilder _hostBuilder;
        private static object _hostBuilderLock = new object();

        private static IHost _host;
        private static object _hostLock = new object();

        private static string _appName;
        private static Version _appVersion;

        #endregion


        #region Public Properties

        /// <summary>
        /// Whether the Service Manager has been Initialized
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                lock (_initializedLock)
                {
                    return _initialized;
                }
            }
            private set
            {
                lock(_initializedLock)
                {
                    _initialized = value;
                }
            }
        }

        /// <summary>
        /// A Global Flag used to Signal the Application Shutdown is in progress
        /// </summary>
        public static bool Shutdown
        {
            get
            {
                lock(_shutdownLock)
                {
                    return _shutdown;
                }
            }
            internal set
            {
                lock(_shutdownLock)
                {
                    _shutdown = value;
                }
            }
        }

        /// <summary>
        /// The Name of the Running Application
        /// </summary>
        public static string AppName
        {
            get
            {
                if(_appName == null)
                {
                    _appName = Assembly.GetEntryAssembly().GetName().Name;
                }

                return _appName;
            }
        }

        /// <summary>
        /// The Version of the Running Application
        /// </summary>
        public static Version AppVersion
        {
            get
            {
                if(_appVersion == null)
                {
                    _appVersion = Assembly.GetEntryAssembly().GetName().Version;
                }

                return _appVersion;
            }
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Initialize the Service (Host) Manager. This should be called at the very start of your Entry Point <c>Main</c> Method
        /// </summary>
        /// <param name="args">The <c>Main</c> Method Startup Arguments</param>
        public static void Initialize(string[] args)
        {
            lock (_initializedLock)
            {
                if (_initialized == false)
                {
                    lock (_hostBuilderLock)
                    {
                        _hostBuilder = createHostBuilder(args);
                    }

                    _initialized = true;
                }
            }
        }

        /// <summary>
        /// Adds a Hosted Service to be Started and Stopped by the Service (Host) Manager
        /// </summary>
        /// <typeparam name="TService">The Service Class</typeparam>
        /// <exception cref="System.InvalidOperationException">Thrown when AddService is called before Initialize</exception>
        public static void AddService<TService>() where TService : class, IHostedService
        {
            if(IsInitialized == false)
            {
                throw new InvalidOperationException("The *Initialize* Method must be called before attempting to Add a Service");
            }

            lock(_hostBuilderLock)
            {
                _hostBuilder.ConfigureServices(services => services.AddHostedService<TService>());
            }
        }

        /// <summary>
        /// Runs the Service Host and Blocks the Calling Thread until a SIGTERM or Ctrl+C is received
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when Run is called before Initialize or when Run is called multiple times within a Service's Lifetime</exception>
        public static void Run()
        {
            if(IsInitialized == false)
            {
                throw new InvalidOperationException("The *Initialize* Method must be called before attempting to Run");
            }

            lock(_hostLock)
            {
                if(_host != null)
                {
                    throw new InvalidOperationException("The *Run* Method can only be called once within a Service's Lifetime");
                }
            }

            lock (_hostLock)
            {
                try
                {
                    lock (_hostBuilderLock)
                    {
                        _host = _hostBuilder.Build();
                    }
                }
                catch(Exception e)
                {
                    Logger.LogCritical(e, "Host Build Exception");

                    if(_host != null)
                    {
                        try
                        {
                            _host.Dispose();
                        }
                        catch
                        {
                        }
                    }

                    return;
                }

                try
                {
                    _host.Run();
                }
                catch (Exception e)
                {
                    Logger.LogCritical(e, "Host Run Exception");
                    
                    return;
                }
                finally
                {
                    _host.Dispose();
                }
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

            builder.ConfigureLogging(loggerFactory => loggerFactory.ClearProviders());

            builder.ConfigureAppConfiguration(configuration => configuration.AddEnvironmentVariables(prefix: "RICADO_"));

            builder.ConfigureServices(services => services.AddHostedService<InternalWorker>());

            return builder;
        }

        #endregion
    }
}
