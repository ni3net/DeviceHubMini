using DeviceHubMini.Client.Contracts;
using DeviceHubMini.Client.GraphQL;
using System.Collections.Concurrent;

namespace DeviceHubMini.Client.Services
{

    public class InMemoryEventStore : IEventStore
    {
        private readonly ConcurrentDictionary<string, byte> _seen = new();
        private long _received;
        private long _dupes;

        public bool IsDuplicate(string eventId)
        {
            Interlocked.Increment(ref _received);
            if (!_seen.TryAdd(eventId, 0))
            {
                Interlocked.Increment(ref _dupes);
                return true;
            }
            return false;
        }

        public void MarkProcessed(string eventId) { /* no-op, already marked */ }

        public long TotalReceived => Interlocked.Read(ref _received);
        public long TotalDuplicates => Interlocked.Read(ref _dupes);
    }
}
