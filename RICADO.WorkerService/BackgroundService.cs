﻿using RICADO.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RICADO.WorkerService
{
    /// <summary>
    /// Provides Support for a Long-Running Task that can have specific Start and Stop requirements
    /// </summary>
    public abstract class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        #region Private Properties

#if !NETSTANDARD
        private PeriodicTimer _timer;
#endif

        private int _minimumDelayBetweenRunCalls = 50;

        #endregion


        #region Protected Properties

        /// <summary>
        /// The Delay in Milliseconds Between consecutive Calls to the <c>Run()</c> Method
        /// </summary>
        /// <remarks>
        /// Defaults to 50ms
        /// </remarks>
        protected int MinimumDelayBetweenRunCalls
        {
            get
            {
                return _minimumDelayBetweenRunCalls;
            }
            set
            {
                _minimumDelayBetweenRunCalls = value;
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
#if !NETSTANDARD
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_minimumDelayBetweenRunCalls));
#endif

            try
            {
                await Start(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, "Unexpected Exception during Background Service Start Async");
                throw;
            }

            await base.StartAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

#if !NETSTANDARD
            _timer?.Dispose();
#endif

            try
            {
                await Stop(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
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
                catch (OperationCanceledException)
                {
                    if(stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogCritical(e, "Unexpected Exception in Background Server Execute Async Method");
                }

                try
                {
#if NETSTANDARD
                    await Task.Delay(_minimumDelayBetweenRunCalls, stoppingToken);
#else
                    await _timer.WaitForNextTickAsync(stoppingToken);
#endif
                }
                catch (OperationCanceledException)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                }
                catch
                {
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
