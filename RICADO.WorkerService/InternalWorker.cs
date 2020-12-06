using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RICADO.Logging;

namespace RICADO.WorkerService
{
    internal class InternalWorker : BackgroundService
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

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Starting " + ServiceManager.AppName);

            return Task.Run(async () =>
            {
                await startTasks(cancellationToken);

                await base.StartAsync(cancellationToken);

                Logger.LogInformation(ServiceManager.AppName + " " + ServiceManager.AppVersion.ToString(3));

                Logger.LogInformation(ServiceManager.AppName + " Started");
            }, cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            ServiceManager.Shutdown = true;

            Logger.LogInformation("Stopping " + ServiceManager.AppName);

            return Task.Run(async () =>
            {
                await base.StopAsync(cancellationToken);

                await stopTasks(cancellationToken);

                Logger.LogInformation(ServiceManager.AppName + " Stopped");
            }, cancellationToken);
        }

        #endregion


        #region Protected Methods

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

        private async Task startTasks(CancellationToken cancellationToken)
        {
            await Task.Delay(1500, cancellationToken); // TODO: Perform Start Tasks
        }

        private async Task stopTasks(CancellationToken cancellationToken)
        {
            await Task.Delay(1500, cancellationToken); // TODO: Perform Stop Tasks
        }

        #endregion
    }
}
