using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Worker.WorkerServices
{
    public class ConfigWatcherWorker : BackgroundService
    {
        private readonly ClientService _clientService;
        private readonly AppSettings _appSettings;
        private readonly string _deviceId;
        private readonly ILogger<ConfigWatcherWorker> _logger;
        public ConfigWatcherWorker(ClientService clientService, AppSettings appSettings, ILogger<ConfigWatcherWorker> logger)
        {
            _clientService = clientService;
            _appSettings = appSettings;
            _deviceId = "Device-001"; // could come from env/config
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var config = await _clientService.GetDeviceConfigAsync(_deviceId, stoppingToken);
                    if (config != null)
                    {
                        // Update the in-memory settings (thread-safe)
                        _appSettings.DeviceConfig = config;
                        _logger.LogInformation($"[ConfigWatcher] Updated config: debounce={config.DebounceMs}, interval={config.DispatchIntervalMs}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"[ConfigWatcher] Failed to refresh config: {ex.Message}");
                }

                // check in as per define in the config (or as configured)
                await Task.Delay(TimeSpan.FromMinutes(_appSettings.ConfigFetchMin), stoppingToken);
            }
        }
    }
}
