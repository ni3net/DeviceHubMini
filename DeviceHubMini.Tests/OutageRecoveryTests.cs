using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Contracts;
using DeviceHubMini.Infrastructure.Entities;
using DeviceHubMini.Infrastructure.Repositories;
using DeviceHubMini.Model;
using DeviceHubMini.Worker.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeviceHubMini.Tests
{
    /// <summary>
    /// Tests how the EventDispatcherService behaves when GraphQL is down and later recovers.
    /// Ensures events are persisted during outage and retried successfully once the service is back.
    /// </summary>
    public class OutageRecoveryTests
    {
        [Fact(DisplayName = "Outage: Events accumulate during GraphQL downtime and dispatch after recovery")]
        public async Task Should_Accumulate_And_Dispatch_After_Recovery()
        {
            // Arrange
            string dbPath = Path.Combine(Path.GetTempPath(), $"outage_{Guid.NewGuid()}.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            var appSettings = new AppSettings
            {
                ServiceDbConnection = $"Data Source={dbPath}",
                DeviceId = "Device-001",
                CommandTimeout = 30
            };

            // 1️⃣ Setup local SQLite repository
            var repo = new DapperRepository(new SqlLiteConnectionFactory(appSettings));
            await repo.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS ScanEvents (
                    EventId TEXT PRIMARY KEY,
                    RawData TEXT,
                    Timestamp TEXT,
                    DeviceId TEXT,
                    Status TEXT,
                    Attempts INT,
                    CreatedAt TEXT
                )");

            // 2️⃣ Mock GraphQL client: fails twice, succeeds the third time
            var gqlMock = new Mock<IGraphQLClientService>();
            gqlMock.SetupSequence(x => x.SendScanEventAsync(It.IsAny<ScanEventEntity>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false)  // 1st attempt fails (network down)
                   .ReturnsAsync(false)  // 2nd attempt fails
                   .ReturnsAsync(true);  // 3rd attempt succeeds (recovered)

            // 3️⃣ Dispatcher under test
            var dispatcher = new EventDispatcherService(
                new ScanDataEventRepository(repo),
                gqlMock.Object,
                NullLogger<EventDispatcherService>.Instance);

            // 4️⃣ Insert 3 pending scan events
            for (int i = 0; i < 3; i++)
            {
                await repo.ExecuteAsync(@"
                    INSERT INTO ScanEvents (EventId, RawData, Timestamp, DeviceId, Status, Attempts, CreatedAt)
                    VALUES (@EventId, @RawData, @Timestamp, @DeviceId, 'Pending', 0, @Timestamp)",
                    new
                    {
                        EventId = Guid.NewGuid().ToString(),
                        RawData = $"DATA-{i}",
                        Timestamp = DateTimeOffset.UtcNow.ToString("O"),
                        DeviceId = appSettings.DeviceId
                    });
            }

            // Act 🔁 Simulate multiple dispatch attempts (2 fails, 1 success)
            for (int i = 0; i < 3; i++)
            {
                await dispatcher.DispatchPendingEventsAsync(CancellationToken.None);
                await Task.Delay(500); // short delay between attempts
            }

            // Assert ✅ All events should be marked as sent
            var remaining = await repo.GetListAsync<ScanEventEntity>(
                "SELECT * FROM ScanEvents WHERE Status = 'Pending'");

            Assert.Empty(remaining); // all should have been dispatched after recovery

            // Cleanup
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}
