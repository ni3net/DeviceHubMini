using System.Threading;
using System.Threading.Tasks;
using DeviceHubMini.Infrastructure.Entities;

namespace DeviceHubMini.Infrastructure.Contracts
{
    public interface IGraphQLClientService
    {
        Task<bool> SendScanEventAsync(ScanEventEntity ev, CancellationToken cancellationToken);
    }
}