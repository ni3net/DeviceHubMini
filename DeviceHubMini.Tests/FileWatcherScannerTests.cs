using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Worker.Scanners;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DeviceHubMini.Tests
{
    public class FileWatcherScannerTests : IDisposable
    {
        private readonly string _testFolder;

        public FileWatcherScannerTests()
        {
            _testFolder = Path.Combine(Path.GetTempPath(), "FileWatcherTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testFolder);
        }

        [Fact]
        public async Task Should_Raise_OnScan_When_New_File_Created()
        {
            var appSettings = new AppSettings
            {
                DeviceId = "D1",
                WatchFolder = _testFolder,
                DeviceConfig = new DeviceConfig { DebounceMs = 200 }
            };

            var logger = NullLogger<FileWatcherScanner>.Instance;
            var scanner = new FileWatcherScanner(appSettings, logger);

            bool eventRaised = false;
            scanner.OnScan += (_, e) =>
            {
                eventRaised = true;
                Assert.Equal("12345", e.RawData);
                Assert.Equal("D1", e.SourceDeviceId);
            };

            await scanner.StartAsync(CancellationToken.None);

            string file = Path.Combine(_testFolder, "scan.txt");
            await File.WriteAllTextAsync(file, "12345");
            await Task.Delay(1000);

            await scanner.StopAsync(CancellationToken.None);

            Assert.True(eventRaised, "Expected OnScan event to be raised.");
            Assert.True(File.Exists(Path.Combine(_testFolder, "processed", "scan.txt")));
        }

        [Fact]
        public async Task Should_Ignore_Duplicates_Within_Debounce_Window()
        {
            var appSettings = new AppSettings
            {
                DeviceId = "D1",
                WatchFolder = _testFolder,
                DeviceConfig = new DeviceConfig { DebounceMs = 500 }
            };

            var logger = NullLogger<FileWatcherScanner>.Instance;
            var scanner = new FileWatcherScanner(appSettings, logger);

            int eventCount = 0;
            scanner.OnScan += (_, _) => eventCount++;

            await scanner.StartAsync(CancellationToken.None);

            string file1 = Path.Combine(_testFolder, "file1.txt");
            string file2 = Path.Combine(_testFolder, "file2.txt");

            await File.WriteAllTextAsync(file1, "ABC123");
            await Task.Delay(200);
            await File.WriteAllTextAsync(file2, "ABC123");

            await Task.Delay(1000);
            await scanner.StopAsync(CancellationToken.None);

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public async Task Should_Move_To_Error_When_File_Locked()
        {
            var appSettings = new AppSettings
            {
                DeviceId = "D1",
                WatchFolder = _testFolder,
                DeviceConfig = new DeviceConfig { DebounceMs = 100 }
            };

            var logger = NullLogger<FileWatcherScanner>.Instance;
            var scanner = new FileWatcherScanner(appSettings, logger);

            await scanner.StartAsync(CancellationToken.None);

            string filePath = Path.Combine(_testFolder, "locked.txt");
            using (var fs = File.Create(filePath))
            {
                await Task.Delay(4200);
            }

            await Task.Delay(150);
            await scanner.StopAsync(CancellationToken.None);

            string errorPath = Path.Combine(_testFolder, "error", "locked.txt");
            Assert.True(File.Exists(errorPath), "File should be moved to error folder after read failure.");
        }

        public void Dispose()
        {
            if (Directory.Exists(_testFolder))
                Directory.Delete(_testFolder, true);
        }
    }
}
