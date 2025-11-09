using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Infrastructure.Entities
{
    public class ScanEventEntity
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public string RawData { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public string DeviceId { get; set; } = string.Empty;

        // Queue state
        public string Status { get; set; } = "Pending"; // Pending|Sent|Failed
        public int Attempts { get; set; } = 0;
        public string? LastError { get; set; }
        public DateTimeOffset? LastTriedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? SentAt { get; set; }
    }
}
