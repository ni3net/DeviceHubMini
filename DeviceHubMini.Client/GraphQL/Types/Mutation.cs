using DeviceHubMini.Client.Contracts;
using DeviceHubMini.Client.GraphQL;
using NewRelic.Api.Agent;
using System.Threading.Tasks;

namespace DeviceHubMini.Client.GraphQL.Types
{
    public class Mutation
    {
        private readonly ILogger<Mutation> _logger;
        private readonly IEventStore _store;

        public Mutation(IEventStore store, ILogger<Mutation> logger)
        {
            _store = store;
            _logger = logger;
        }

        [Trace]
        public async Task<ScanResult> SendScan(ScanInput input)
        {
            var processedAt = DateTimeOffset.UtcNow;

            try
            {
                // 1️⃣ Log the incoming event
                _logger.LogInformation("EventId = {EventId} | Received scan request", input.EventId, input.DeviceId, input.Code, input.CapturedAt);

                // 2️⃣ Check for duplicates
                var duplicate = _store.IsDuplicate(input.EventId);
                if (duplicate)
                {
                    _logger.LogWarning("EventId={EventId} | Duplicate scan detecte", input.EventId);
                }
                else
                {
                    _store.MarkProcessed(input.EventId);
                    _logger.LogInformation("EventId = {EventId} | Scan processed successfully", input.EventId);
                    // Dealy the process to mimic the duplicate case
                   await Task.Delay(5000);
                }

                

                return new ScanResult(true, "ok", processedAt);
            }
            catch (Exception ex)
            {
                // 4️⃣ Log any unexpected errors
                _logger.LogError(ex, "EventId = {EventId} | Error processing scan | DeviceId={DeviceId}", input.EventId, input.DeviceId);
                return new ScanResult(false, ex.Message, processedAt);
            }
        }
    }
}
