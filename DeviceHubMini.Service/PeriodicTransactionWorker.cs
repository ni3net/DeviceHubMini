using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DeviceHubMini.Service
{
    public class PeriodicTransactionWorker : BackgroundService
    {
        private readonly ILogger<PeriodicTransactionWorker> _logger;

        public PeriodicTransactionWorker(ILogger<PeriodicTransactionWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create periodic transaction to keep New Relic active
                    await PerformHealthCheck();
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in periodic transaction worker");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        [Transaction]
        [Trace]
        [MethodImpl(MethodImplOptions.NoInlining)]

        private async Task PerformHealthCheck()
        {
            _logger.LogInformation("Service health check - {Timestamp}", DateTime.UtcNow);
            await Task.Delay(100); // Simulate work
        }
    }
}