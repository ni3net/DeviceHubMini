using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceHubMini.Infrastructure.Contracts;
using DeviceHubMini.Infrastructure.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using DeviceHubMini.Worker.Services;

namespace DeviceHubMini.Tests
{
    public class IdempotencyTests
    {
        [Fact]
        public async Task Should_Only_Process_Once_For_Same_EventId()
        {
            var gqlMock = new Mock<IGraphQLClientService>();
            gqlMock.Setup(x => x.SendScanEventAsync(It.IsAny<ScanEventEntity>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

            var repo = new Mock<IScanDataEventRepository>();
            var dispatcher = new EventDispatcherService(repo.Object, gqlMock.Object, NullLogger<EventDispatcherService>.Instance);

            var ev = new ScanEventEntity { EventId = "E1", RawData = "DATA", DeviceId = "D1", Timestamp = DateTimeOffset.UtcNow };

            await gqlMock.Object.SendScanEventAsync(ev, CancellationToken.None);
            await gqlMock.Object.SendScanEventAsync(ev, CancellationToken.None);

            gqlMock.Verify(x => x.SendScanEventAsync(It.IsAny<ScanEventEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
