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

        public static void AddService<TService>() where TService : class, IHostedService
        {
            if(IsInitialized == false)
            {
                throw new Exception("Initialize hasn't been called!"); // TODO: Improve this Exception
            }

            lock(_hostBuilderLock)
            {
                _hostBuilder.ConfigureServices(services => services.AddHostedService<TService>());
            }
        }

        public static void Run()
        {
            if(IsInitialized == false)
            {
                throw new Exception("Initialize hasn't been called!"); // TODO: Improve this Exception
            }

            lock(_hostLock)
            {
                if(_host != null)
                {
                    throw new Exception("Run can only be called once!"); // TODO: Improve this Exception
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
