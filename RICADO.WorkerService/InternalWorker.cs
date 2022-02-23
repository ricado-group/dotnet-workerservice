using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using RICADO.Logging;

namespace RICADO.WorkerService
{
    internal class InternalWorker : Microsoft.Extensions.Hosting.BackgroundService
    {
        #region Private Properties

        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfigurationRoot _configuration;
        private readonly Version _version;

        #endregion


        #region Constructor

        public InternalWorker(IHostEnvironment hostEnvironment, IConfiguration configuration)
        {
            _hostEnvironment = hostEnvironment;

            _configuration = (IConfigurationRoot)configuration;

            _version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0,0,0);
        }

        #endregion


        #region Public Methods

        /// <inheritdoc/>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            LogManager.Initialize(_configuration.GetSection("Logging").GetValue<string>("Level", "Information"), _hostEnvironment.EnvironmentName, _hostEnvironment.ContentRootPath);

            Logger.LogInformation("Starting {name}", _hostEnvironment.ApplicationName);

            Logger.LogInformation("Environment: {env}", _hostEnvironment.EnvironmentName);

            Logger.LogInformation("Version: {version}", _version.ToString(3));

            Configuration.ConfigurationProvider.Initialize(_configuration);

            configureBugsnag();

            await base.StartAsync(cancellationToken);

            Logger.LogInformation("{name} Started", _hostEnvironment.ApplicationName);
        }

        /// <inheritdoc/>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping {name}", _hostEnvironment.ApplicationName);

            await base.StopAsync(cancellationToken);

            Configuration.ConfigurationProvider.Destroy();

            Logger.LogInformation("{name} Stopped", _hostEnvironment.ApplicationName);

            LogManager.Destroy();
        }

        #endregion


        #region Protected Methods

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch
                {
                }
            }
        }

        #endregion


        #region Private Methods

        private static void configureBugsnag()
        {
            try
            {
                if (Configuration.ConfigurationProvider.TrySelectValue("Bugsnag.APIKey", null, out string apiKey))
                {
                    LogManager.ConfigureBugsnag(apiKey);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Unexpected Exception during Bugsnag Configuration");
            }
        }

        #endregion
    }
}
