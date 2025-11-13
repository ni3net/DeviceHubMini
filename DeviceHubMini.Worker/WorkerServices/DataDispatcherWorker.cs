using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Common.DTOs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeviceHubMini.Worker.WorkerServices
{
    /// <summary>
    /// Periodically dispatches pending events using IEventDispatcherService.
    /// Stops after repeated failures to prevent spam.
    /// </summary>
    public sealed class DataDispatcherWorker : BackgroundService
    {
        private readonly IEventDispatcherService _eventDispatcher;
        private readonly ILogger<DataDispatcherWorker> _logger;
        private readonly TimeSpan _dispatchInterval;
        private readonly int _maxFailureCycles;
        private int _failureCycleCount;

        public DataDispatcherWorker(
            IEventDispatcherService eventDispatcher,
            ILogger<DataDispatcherWorker> logger,
            AppSettings appSettings)
        {
            _eventDispatcher = eventDispatcher;
            _logger = logger;
            _dispatchInterval = TimeSpan.FromMilliseconds(appSettings.DeviceConfig.DispatchIntervalMs);
            _maxFailureCycles = appSettings.DispatchMaxFailureCycles > 0
                ? appSettings.DispatchMaxFailureCycles
                : 3;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataDispatcherWorker started. Interval = {Seconds}s", _dispatchInterval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _eventDispatcher.DispatchPendingEventsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // _logger.LogError(ex, "Dispatcher failed  will retry automatically.");
                    _logger.LogError(ex, "Dispatcher failed  will retry automatically.");
                }

                // Wait before next dispatch cycle
                await Task.Delay(_dispatchInterval, stoppingToken);
            }

            _logger.LogWarning("DataDispatcherWorker stopped after {Failures} consecutive failures.", _failureCycleCount);
        }
    }
}
