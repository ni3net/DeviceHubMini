using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceHubMini.Infrastructure.Entities;
using DeviceHubMini.Infrastructure.GraphQL;
using DeviceHubMini.Infrastructure.Repositories;
using DeviceHubMini.Worker.Interfaces;
using Microsoft.Extensions.Logging;

namespace DeviceHubMini.Worker.Services
{
    public sealed class EventDispatcherService : IEventDispatcherService
    {
        private readonly IScanDataEventRepository _eventRepo;
        private readonly IGraphQLClientService _graphqlClient;
        private readonly ILogger<EventDispatcherService> _logger;

        public EventDispatcherService(
            IScanDataEventRepository eventRepo,
            IGraphQLClientService graphqlClient,
            ILogger<EventDispatcherService> logger)
        {
            _eventRepo = eventRepo;
            _graphqlClient = graphqlClient;
            _logger = logger;
        }

        public async Task<bool> DispatchPendingEventsAsync(CancellationToken ct)
        {
            var pending = await _eventRepo.GetPendingEventsAsync(25);
            if (pending == null || pending.Count == 0)
                return true;

            int successCount = 0;

            foreach (var ev in pending)
            {
                if (ct.IsCancellationRequested) break;

                bool sent = await _graphqlClient.SendScanEventAsync(ev, ct);

                if (sent)
                {
                    await _eventRepo.MarkAsSentAsync(ev.EventId, DateTimeOffset.UtcNow);
                    successCount++;
                }
                else
                {
                    await _eventRepo.MarkAsFailedAsync(ev.EventId, "Failed to send to GraphQL");
                }
            }

            _logger.LogInformation("Dispatch batch complete: {Success}/{Total} sent.", successCount, pending.Count);
            return successCount > 0;
        }
    }
}
