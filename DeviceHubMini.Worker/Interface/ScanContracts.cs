using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Jobs.Interface
{
    public record ScanEvent(string RawData, DateTimeOffset Timestamp, string SourceDeviceId);

    public interface IScanDevice
    {
        event EventHandler<ScanEvent> OnScan;
        Task StartAsync(CancellationToken ct);
        Task StopAsync(CancellationToken ct);
    }
}
