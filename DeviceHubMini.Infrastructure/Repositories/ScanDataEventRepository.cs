using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeviceHubMini.Infrastructure.Entities;
using Dapper;

namespace DeviceHubMini.Infrastructure.Repositories
{
    public sealed class ScanDataEventRepository : IScanDataEventRepository
    {
        private readonly IRepository _repository;

        public ScanDataEventRepository(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<ScanEventEntity>> GetPendingEventsAsync(int limit = 25)
        {
            const string sql = @"
SELECT * 
FROM ScanEvents
WHERE Status = 'Pending'
ORDER BY CreatedAt
LIMIT @Limit;";

            return await _repository.GetListAsync<ScanEventEntity>(sql, new { Limit = limit });
        }

        public async Task MarkAsSentAsync(Guid eventId, DateTimeOffset sentAt)
        {
            const string sql = @"
UPDATE ScanEvents
SET Status = 'Sent',
    SentAt = @SentAt,
    LastError = NULL
WHERE EventId = @EventId;";

            await _repository.ExecuteAsync(sql, new { EventId = eventId, SentAt = sentAt });
        }

        public async Task MarkAsFailedAsync(Guid eventId, string errorMessage)
        {
            const string sql = @"
UPDATE ScanEvents
SET Attempts = Attempts + 1,
    LastError = @Error,
    Status = 'Pending'
WHERE EventId = @EventId;";

            await _repository.ExecuteAsync(sql, new { EventId = eventId, Error = errorMessage });
        }
    }
}
