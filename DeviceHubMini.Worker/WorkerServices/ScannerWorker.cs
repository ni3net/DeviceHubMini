using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Infrastructure.Contracts;
using DeviceHubMini.Infrastructure.Entities;
using DeviceHubMini.Jobs.Interface;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHubMini.Worker.WorkerServices
{
    public sealed class ScannerWorker : BackgroundService
    {
        private readonly IScanDevice _scanner;
        private readonly IScanDataEventRepository _eventRepository;
        private readonly ILogger<ScannerWorker> _logger;

        public ScannerWorker(
            IScanDevice scanner,
            IScanDataEventRepository eventRepository,
            ILogger<ScannerWorker> logger)
        {
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScannerWorker started. Listening for scanned events...");

            _scanner.OnScan += async (_, e) =>
            {
                try
                {
                    var scanEvent = new ScanEventEntity
                    {
                        EventId = Guid.NewGuid().ToString(),
                        RawData = e.RawData,
                        Timestamp = e.Timestamp,
                        DeviceId = e.SourceDeviceId,
                        Status = "Pending",
                        Attempts = 0,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    await _eventRepository.AddScanEventAsync(scanEvent);

                    _logger.LogInformation("Inserted scan event {EventId} from device {DeviceId}", scanEvent.EventId, scanEvent.DeviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving scanned event from device {DeviceId}", e.SourceDeviceId);
                }
            };

            await _scanner.StartAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ScannerWorker stopping...");
            await _scanner.StopAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
