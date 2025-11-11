using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHubMini.Worker.WorkerServices
{
    /// <summary>
    /// Periodically polls the GraphQL API for updated device configuration.
    /// Updates in-memory AppSettings when new config is fetched.
    /// </summary>
    public class ConfigWatcherWorker : BackgroundService
    {
        private readonly IGraphQLClientService _graphqlClientService;
        private readonly AppSettings _appSettings;
        private readonly string _deviceId;
        private readonly ILogger<ConfigWatcherWorker> _logger;

        public ConfigWatcherWorker(
            IGraphQLClientService graphqlClientService,
            AppSettings appSettings,
            ILogger<ConfigWatcherWorker> logger)
        {
            _graphqlClientService = graphqlClientService;
            _appSettings = appSettings;
            _logger = logger;

            // Device ID can come from configuration or environment
            _deviceId = _appSettings.DeviceId ?? "Device-001";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ConfigWatcherWorker started. DeviceId = {DeviceId}", _deviceId);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Fetch latest config from GraphQL
                    var config = await _graphqlClientService.GetDeviceConfigAsync(_deviceId, stoppingToken);

                    if (config != null)
                    {
                        // Thread-safe update of current device configuration
                        _appSettings.DeviceConfig = config;
                        _logger.LogInformation(
                            "[ConfigWatcher] Updated config: debounce={Debounce}ms, interval={Interval}ms",
                            config.DebounceMs,
                            config.DispatchIntervalMs
                        );
                    }
                    else
                    {
                        _logger.LogWarning("[ConfigWatcher] No config received from server for {DeviceId}", _deviceId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ConfigWatcher] Failed to refresh config for {DeviceId}", _deviceId);
                }

                // Delay for configured interval
                var delayMinutes = Math.Max(1, _appSettings.ConfigFetchMin);
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }

            _logger.LogInformation("ConfigWatcherWorker stopped.");
        }
    }
}
