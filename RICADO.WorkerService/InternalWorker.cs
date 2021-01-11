using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using RICADO.Logging;
using RICADO.Configuration;

namespace RICADO.WorkerService
{
    internal class InternalWorker : Microsoft.Extensions.Hosting.BackgroundService
    {
        #region Private Properties

        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfigurationRoot _configuration;
        private readonly Version _version;

        #endregion


        #region Public Properties


        #endregion


        #region Constructor

        public InternalWorker(IHostEnvironment hostEnvironment, IConfiguration configuration)
        {
            _hostEnvironment = hostEnvironment;

            _configuration = (IConfigurationRoot)configuration;

            _version = Assembly.GetEntryAssembly().GetName().Version;
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

            RICADO.Configuration.ConfigurationProvider.Initialize(_configuration);

            configureBugsnag();

            await base.StartAsync(cancellationToken);

            Logger.LogInformation("{name} Started", _hostEnvironment.ApplicationName);
        }

        /// <inheritdoc/>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping {name}", _hostEnvironment.ApplicationName);

            await base.StopAsync(cancellationToken);

            RICADO.Configuration.ConfigurationProvider.Destroy();

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
                    await Task.Delay(15000, stoppingToken);
                }
                catch
                {
                }
            }
        }

        #endregion


        #region Private Methods

        private void configureBugsnag()
        {
            try
            {
                string apiKey;

                if (RICADO.Configuration.ConfigurationProvider.TrySelectValue<string>("Bugsnag.APIKey", null, out apiKey))
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
