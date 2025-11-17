using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Jobs.Interface;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHubMini.Worker.Scanners
{
    /// <summary>
    /// Mock Bluetooth scanner that simulates barcode scans periodically.
    /// Used for testing without a real Bluetooth device.
    /// </summary>
    public sealed class BluetoothScannerMock : IScanDevice
    {
        private readonly ILogger<BluetoothScannerMock> _logger;
        private readonly AppSettings _appSettings;
        private readonly string _deviceId;
        private bool _running;

        public event EventHandler<ScanEvent>? OnScan;

        public BluetoothScannerMock(AppSettings appSettings, ILogger<BluetoothScannerMock> logger)
        {
            _appSettings = appSettings;
            _logger = logger;
            _deviceId = appSettings.DeviceId ?? "BT-Device-001";
        }
       
      
        public Task StartAsync(CancellationToken ct)
        {
            _running = true;
            _logger.LogInformation("BluetoothScannerMock started for device {DeviceId}", _deviceId);

            // Start background scan simulation
            _ = Task.Run(async () =>
            {
                var random = new Random();
                while (_running && !ct.IsCancellationRequested)
                {
                    try
                    {
                       await ExecuteBTEvent(random, ct);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during simulated Bluetooth scan loop.");
                    }
                }

                _logger.LogInformation("BluetoothScannerMock stopped scan loop.");
            }, ct);

            return Task.CompletedTask;
        }
        [Transaction]
        [Trace]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task ExecuteBTEvent(Random random, CancellationToken ct)
        {
            // Simulate scan every 3–5 seconds
            var delay = random.Next(2000, 5000);
            await Task.Delay(delay, ct);

            var code = $"BT-{random.Next(100000, 999999)}";
            var scanEvent = new ScanEvent(code, DateTimeOffset.UtcNow, _deviceId);

            _logger.LogInformation("Simulated Bluetooth scan: {Code}", code);
            OnScan?.Invoke(this, scanEvent);
        }

        public Task StopAsync(CancellationToken ct)
        {
            _running = false;
            _logger.LogInformation("BluetoothScannerMock stopping...");
            return Task.CompletedTask;
        }
    }
}
