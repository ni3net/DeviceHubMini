using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Contracts;
using DeviceHubMini.Worker.WorkerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DeviceHubMini.Tests
{
    public class ConfigWatcherTests
    {
        [Fact(DisplayName = "ConfigWatcher: Should hot-reload debounce setting without restart")]
        public async Task Should_HotReload_Config_Without_Restart()
        {
            // Arrange
            var appSettings = new AppSettings
            {
                DeviceId = "Device-001",
                ConfigFetchMin = 1, // run every 3 seconds instead of 1 minute
                DeviceConfig = new DeviceConfig { DebounceMs = 200, DispatchIntervalMs = 500 }
            };

            var gqlMock = new Mock<IGraphQLClientService>();

            // First call returns old config, second call returns updated config
            gqlMock.SetupSequence(x => x.GetDeviceConfigAsync("Device-001", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new DeviceConfig { DebounceMs = 200, DispatchIntervalMs = 500 })
                   .ReturnsAsync(new DeviceConfig { DebounceMs = 100, DispatchIntervalMs = 500 });

            var worker = new ConfigWatcherWorker(gqlMock.Object, appSettings, NullLogger<ConfigWatcherWorker>.Instance);

            // Act
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(61)); // give time for 2 cycles
            var task = worker.StartAsync(cts.Token);

            await Task.Delay(61000); // allow 2 full refresh cycles

            // Assert
            Assert.Equal(100, appSettings.DeviceConfig.DebounceMs);
        }
    }
}
