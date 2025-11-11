using Dapper;
using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Contracts;
using DeviceHubMini.Infrastructure.Entities;
using DeviceHubMini.Infrastructure.Handler;
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
    /// Ensures that unsent events remain in the SQLite queue when the service stops,
    /// and are successfully dispatched after restart.
    /// </summary>
    public class QueueDurabilityTests
    {
        [Fact(DisplayName = "Durability: Unsent events persist and dispatch after restart")]
        public async Task UnsentEvents_Persist_And_Dispatch_After_Restart()
        {
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            // Arrange
            string dbPath = Path.Combine(Path.GetTempPath(), $"durability_{Guid.NewGuid()}.db");
            if (File.Exists(dbPath)) File.Delete(dbPath);

            var appSettings = new AppSettings
            {
                ServiceDbConnection = $"Data Source={dbPath}",
                DeviceId = "Device-001",
                CommandTimeout = 30
            };

            // 1️⃣ Initialize repository and schema
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

            // 2️⃣ Mock GraphQL client (down first, then recovers)
            var gqlMock = new Mock<IGraphQLClientService>();
            gqlMock.SetupSequence(x => x.SendScanEventAsync(It.IsAny<ScanEventEntity>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false)  // service down
                   .ReturnsAsync(true);  // recovered

            // 3️⃣ Dispatcher service under test
            var dispatcher = new EventDispatcherService(
                new ScanDataEventRepository(repo),
                gqlMock.Object,
                NullLogger<EventDispatcherService>.Instance);

            // 4️⃣ Insert fake unsent event
            var ev = new ScanEventEntity
            {
                EventId = Guid.NewGuid().ToString(),
                RawData = "XYZ123",
                Timestamp = DateTimeOffset.UtcNow,
                DeviceId = appSettings.DeviceId,
                Status = "Pending",
                Attempts = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await repo.ExecuteAsync(@"
                INSERT INTO ScanEvents (EventId, RawData, Timestamp, DeviceId, Status, Attempts, CreatedAt)
                VALUES (@EventId, @RawData, @Timestamp, @DeviceId, @Status, @Attempts, @CreatedAt)", ev);

            // Act 1️⃣ — First attempt fails (network outage)
            await dispatcher.DispatchPendingEventsAsync(CancellationToken.None);

            // Validate that record is still pending
            var beforeRestart = await repo.GetListAsync<ScanEventEntity>(
                "SELECT * FROM ScanEvents WHERE Status = 'Pending'");
            Assert.Single(beforeRestart);

            // Act 2️⃣ — Simulate service restart
            var dispatcher2 = new EventDispatcherService(
                new ScanDataEventRepository(repo),
                gqlMock.Object,
                NullLogger<EventDispatcherService>.Instance);

            await dispatcher2.DispatchPendingEventsAsync(CancellationToken.None);

            // Assert ✅ After restart, record should be sent successfully
            var remaining = await repo.GetListAsync<ScanEventEntity>(
                "SELECT * FROM ScanEvents WHERE Status = 'Pending'");
            Assert.Empty(remaining);

            await Task.Delay(300);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Cleanup
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (File.Exists(dbPath))
                        File.Delete(dbPath);
                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(200);
                }
            }
        }
    }
}
