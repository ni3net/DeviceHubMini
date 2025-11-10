using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Entities;
using DeviceHubMini.Worker.WorkerServices;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class DataDispatcherWorkerTests
{
    //[Fact]
    //public async Task Should_Update_Event_After_Successful_Send()
    //{
    //    // Arrange
    //    var mockRepo = new Mock<IRepository>();
    //    var mockLogger = new Mock<ILogger<DataDispatcherWorker>>();

    //    var events = new List<ScanEventEntity>
    //    {
    //        new() { EventId = Guid.NewGuid().ToString(), RawData = "CODE123", DeviceId = "D1", Status = "Pending" }
    //    };

    //    // Mock pending events
    //    mockRepo.Setup(r => r.GetListAsync<ScanEventEntity>(It.IsAny<string>(), null))
    //            .ReturnsAsync(events);

    //    // Mock ExecuteAsync to print out SQL executed (for debug)
    //    mockRepo.Setup(r => r.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
    //            .Callback<string, object>((sql, param) =>
    //            {
    //                Console.WriteLine($"Executed SQL: {sql}");
    //            })
    //            .ReturnsAsync(1);

    //    var appSettings = new AppSettings
    //    {
    //        GraphQLUrl = "http://localhost:5068/graphql",
    //        GraphQLApiKey = "dev-key-123",
    //        DeviceConfig = new DeviceConfig { DispatchIntervalMs = 2000 },
    //        DispatchMaxFailureCycles = 3
    //    };

    //    var worker = new DataDispatcherWorker(mockRepo.Object, mockLogger.Object, appSettings);

    //    // Act
    //    await worker.StartAsync(CancellationToken.None);
    //    await Task.Delay(2000); // small delay to allow one dispatch cycle
    //    await worker.StopAsync(CancellationToken.None);

    //    // Assert → verify UPDATE was executed
    //    mockRepo.Verify(
    //        r => r.ExecuteAsync(It.Is<string>(s => s.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)), It.IsAny<object>()),
    //        Times.AtLeastOnce
    //    );
    //}
}
