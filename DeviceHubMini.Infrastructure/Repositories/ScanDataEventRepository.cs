using Dapper;
using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Infrastructure.Contracts;
using DeviceHubMini.Infrastructure.Entities;
using NewRelic.Api.Agent;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DeviceHubMini.Infrastructure.Repositories
{
    public sealed class ScanDataEventRepository : IScanDataEventRepository
    {
        private readonly IRepository _repository;

        public ScanDataEventRepository(IRepository repository)
        {
            _repository = repository;
        }
        [Trace]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<IEnumerable<ScanEventEntity>> GetPendingEventsAsync(int limit = 25)
        {
            const string sql = @"
SELECT * 
FROM ScanEvents
WHERE Status = 'Pending'
ORDER BY CreatedAt
LIMIT @Limit;";

            return await _repository.GetListAsync<ScanEventEntity>(sql, new { Limit = limit });
        }
        [Trace]
        [MethodImpl(MethodImplOptions.NoInlining)]

        public async Task MarkAsSentAsync(string eventId, DateTimeOffset sentAt)
        {
            const string sql = @"
UPDATE ScanEvents
SET Status = 'Sent',
    SentAt = @SentAt,
    LastError = NULL
WHERE EventId = @EventId;";

            await _repository.ExecuteAsync(sql, new { EventId = eventId, SentAt = sentAt });
        }
        [Trace]
        [MethodImpl(MethodImplOptions.NoInlining)]

        public async Task MarkAsFailedAsync(string eventId, string errorMessage)
        {
            const string sql = @"
UPDATE ScanEvents
SET Attempts = Attempts + 1,
    LastError = @Error,
    Status = 'Pending'
WHERE EventId = @EventId;";

            await _repository.ExecuteAsync(sql, new { EventId = eventId, Error = errorMessage });
        }

        [Trace]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task AddScanEventAsync(ScanEventEntity scanEvent)
        {
            const string sql = @"
INSERT INTO ScanEvents
(EventId, RawData, Timestamp, DeviceId, Status, Attempts, CreatedAt)
VALUES (@EventId, @RawData, @Timestamp, @DeviceId, @Status, @Attempts, @CreatedAt);";

            await _repository.ExecuteAsync(sql, scanEvent);
        }
    }
}
