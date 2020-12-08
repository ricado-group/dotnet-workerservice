using System;
using System.Threading;
using System.Threading.Tasks;
using RICADO.Logging;

namespace RICADO.WorkerService
{
    internal class InternalWorker : Microsoft.Extensions.Hosting.BackgroundService
    {
        #region Private Properties


        #endregion


        #region Public Properties


        #endregion


        #region Constructor

        public InternalWorker()
        {
        }

        #endregion


        #region Public Methods

        /// <inheritdoc/>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Starting " + ServiceManager.AppName);

            await Task.Delay(1500, cancellationToken); // TODO: Perform Start Tasks

            await base.StartAsync(cancellationToken);

            Logger.LogInformation(ServiceManager.AppName + " " + ServiceManager.AppVersion.ToString(3));

            Logger.LogInformation(ServiceManager.AppName + " Started");
        }

        /// <inheritdoc/>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            ServiceManager.Shutdown = true;

            Logger.LogInformation("Stopping " + ServiceManager.AppName);

            await base.StopAsync(cancellationToken);

            await Task.Delay(1500, cancellationToken); // TODO: Perform Stop Tasks

            Logger.LogInformation(ServiceManager.AppName + " Stopped");
        }

        #endregion


        #region Protected Methods

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(stoppingToken.IsCancellationRequested == false && ServiceManager.Shutdown == false)
            {
                try
                {
                    await Task.Delay(1000, stoppingToken);
                }
                catch
                {
                }
            }
        }

        #endregion


        #region Private Methods


        #endregion
    }
}
