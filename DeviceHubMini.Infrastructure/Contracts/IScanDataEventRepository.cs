using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeviceHubMini.Infrastructure.Entities;

namespace DeviceHubMini.Infrastructure.Contracts
{
    public interface IScanDataEventRepository
    {
        Task<IEnumerable<ScanEventEntity>> GetPendingEventsAsync(int limit = 25);
        Task MarkAsSentAsync(string eventId, DateTimeOffset sentAt);
        Task MarkAsFailedAsync(string eventId, string errorMessage);
    }
}