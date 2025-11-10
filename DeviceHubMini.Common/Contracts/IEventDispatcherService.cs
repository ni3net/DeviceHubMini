using System.Threading;
using System.Threading.Tasks;

namespace DeviceHubMini.Common.Contracts
{
    public interface IEventDispatcherService
    {
        Task<bool> DispatchPendingEventsAsync(CancellationToken cancellationToken);
    }
}
