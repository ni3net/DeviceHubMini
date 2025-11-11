using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Infrastructure.Entities;
using DeviceHubMini.Jobs.Interface;
using DeviceHubMini.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Worker.WorkerServices
{
    public sealed class ScannerWorker : BackgroundService
    {
        private readonly IScanDevice _scanner;
        private readonly IRepository _repository;
        private readonly ILogger<ScannerWorker> _logger;

        public ScannerWorker(
            IScanDevice scanner,
            IRepository repository,
            ILogger<ScannerWorker> logger)
        {
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScannerWorker started. Listening for scanned events...");

            // Attach event handler for scan device
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

                    const string sql = @"
INSERT INTO ScanEvents
(EventId, RawData, Timestamp, DeviceId, Status, Attempts, CreatedAt)
VALUES (@EventId, @RawData, @Timestamp, @DeviceId, @Status, @Attempts, @CreatedAt);";

                    // Use repository to insert event
                    await _repository.ExecuteAsync(sql, scanEvent);

                    _logger.LogInformation("Inserted scan event {EventId} from device {DeviceId}", scanEvent.EventId, scanEvent.DeviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inserting scanned event into SQLite");
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
