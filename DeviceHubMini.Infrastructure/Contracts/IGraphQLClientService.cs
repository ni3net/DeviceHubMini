using System.Threading;
using System.Threading.Tasks;
using DeviceHubMini.Infrastructure.Entities;
using DeviceHubMini.Common.DTOs;

namespace DeviceHubMini.Infrastructure.Contracts
{
    public interface IGraphQLClientService
    {
        /// <summary>
        /// Sends a scanned event to the GraphQL API (mutation).
        /// </summary>
        Task<bool> SendScanEventAsync(ScanEventEntity ev, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches the device configuration from the GraphQL API (query).
        /// </summary>
        Task<DeviceConfig?> GetDeviceConfigAsync(string deviceId, CancellationToken cancellationToken);
    }
}
