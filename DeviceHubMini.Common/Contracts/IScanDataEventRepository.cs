using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeviceHubMini.Infrastructure.Entities;

namespace DeviceHubMini.Common.Contracts
{
    public interface IScanDataEventRepository
    {
        Task<IReadOnlyList<ScanEventEntity>> GetPendingEventsAsync(int limit = 25);
        Task MarkAsSentAsync(Guid eventId, DateTimeOffset sentAt);
        Task MarkAsFailedAsync(Guid eventId, string errorMessage);
    }
}