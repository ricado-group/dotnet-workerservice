using System;
using System.Threading;
using System.Threading.Tasks;
using RICADO.Logging;

namespace RICADO.WorkerService
{
    /// <summary>
    /// Provides Support for a Long-Running Task that can have specific Start and Stop requirements
    /// </summary>
    public abstract class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        #region Private Properties

        private int _minimumDelayBetweenRunCalls = 50;
        private object _minimumDelayBetweenCallsLock = new object();

        #endregion


        #region Public Properties

        /// <summary>
        /// The Delay in Milliseconds Between consecutive Calls to the <c>Run()</c> Method
        /// </summary>
        /// <remarks>
        /// Defaults to 50ms
        /// </remarks>
        public int MinimumDelayBetweenRunCalls
        {
            get
            {
                lock(_minimumDelayBetweenCallsLock)
                {
                    return _minimumDelayBetweenRunCalls;
                }
            }
            set
            {
                lock(_minimumDelayBetweenCallsLock)
                {
                    _minimumDelayBetweenRunCalls = value;
                }
            }
        }

        #endregion


        #region Constructor

        /// <summary>
        /// Create a New <c>BackgroundService</c>
        /// </summary>
        public BackgroundService()
        {
        }

        #endregion


        #region Public Methods

        /// <inheritdoc/>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Start(cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, "Unexpected Exception during Background Service Start Async");
            }

            await base.StartAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

            try
            {
                await Stop(cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, "Unexpected Exception during Background Service Stop Async");
            }
        }

        #endregion


        #region Protected Methods

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    await Run(stoppingToken);
                }
                catch (Exception e)
                {
                    Logger.LogCritical(e, "Unexpected Exception in Background Server Execute Async Method");
                }
                finally
                {
                    try
                    {
                        await Task.Delay(MinimumDelayBetweenRunCalls, stoppingToken);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// This Method is called when the <c>BackgroundService</c> is Starting
        /// </summary>
        /// <example>
        /// <code>
        /// public override async Task Start(CancellationToken cancellationToken)
        /// {
        ///     await Task.Delay(1000, cancellationToken); // Replace with awaitable Start Work
        /// }
        /// </code>
        /// </example>
        /// <param name="cancellationToken">A Cancellation Token that should be used by all Tasks</param>
        protected abstract Task Start(CancellationToken cancellationToken);

        /// <summary>
        /// This Method is called constantly (from inside a while loop) throughout the Lifetime of this Service
        /// </summary>
        /// <remarks>
        /// Typically it would be recommended to complete a small unit of work each call and adjust the Minimum Delay between Calls to suit
        /// </remarks>
        /// <example>
        /// <code>
        /// public override async Task Run(CancellationToken stoppingToken)
        /// {
        ///     await someAwaitableTask(stoppingToken);
        ///     await Task.Run(() => {
        ///         // Some non-awaitable task
        ///     }, stoppingToken);
        /// }
        /// </code>
        /// </example>
        /// <param name="stoppingToken">A Cancellation Token that should be used by all Tasks</param>
        protected abstract Task Run(CancellationToken stoppingToken);

        /// <summary>
        /// This Method is called when the <c>BackgroundService</c> is Stopping
        /// </summary>
        /// <example>
        /// <code>
        /// public override async Task Stop(CancellationToken cancellationToken)
        /// {
        ///     await Task.Delay(1000, cancellationToken); // Replace with awaitable Stop Work
        /// }
        /// </code>
        /// </example>
        /// <param name="cancellationToken">A Cancellation Token that should be used by all Tasks</param>
        protected abstract Task Stop(CancellationToken cancellationToken);

        #endregion


        #region Private Methods


        #endregion
    }
}
