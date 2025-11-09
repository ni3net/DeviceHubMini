using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Entities;
using DeviceHubMini.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeviceHubMini.Worker.WorkerServices
{
    /// <summary>
    /// Periodically scans SQLite for Pending events and sends them to GraphQL API.
    /// Stops itself after repeated failures to avoid log spam and server overload.
    /// </summary>
    public sealed class DataDispatcherWorker : BackgroundService
    {
        private readonly IRepository _repository;
        private readonly ILogger<DataDispatcherWorker> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _graphqlUrl;
        private readonly string _apiKey;
        private readonly TimeSpan _dispatchInterval;
        private readonly int _maxAttempts;          // per-event attempts
        private readonly int _maxFailureCycles;     // consecutive failed cycles before stopping
        private int _failureCycleCount = 0;

        public DataDispatcherWorker(
            IRepository repository,
            ILogger<DataDispatcherWorker> logger,
            AppSettings appSettings)
        {
            _repository = repository;
            _logger = logger;
            _httpClient = new HttpClient();
            _graphqlUrl = appSettings.GraphQLUrl;
            _apiKey = appSettings.GraphQLApiKey;
            _dispatchInterval = TimeSpan.FromSeconds(appSettings.DeviceConfig.DispatchIntervalMs/1000);
         
           // _maxFailureCycles = appSettings.DispatchMaxFailureCycles;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataDispatcherWorker started. Polling interval = {Seconds} seconds", _dispatchInterval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                bool success = true;

                try
                {
                    success = await DispatchPendingEventsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in DispatchPendingEventsAsync");
                    success = false;
                }

                // ✅ Failure tracking logic
                //if (success)
                //{
                //    if (_failureCycleCount > 0)
                //    {
                //        _logger.LogInformation("Dispatcher recovered after {Count} consecutive failures.", _failureCycleCount);
                //    }
                //    _failureCycleCount = 0;
                //}
                //else
                //{
                //    _failureCycleCount++;
                //    _logger.LogWarning("Dispatch cycle failed ({Count}/{MaxCycles}).", _failureCycleCount, _maxFailureCycles);

                //    if (_failureCycleCount >= _maxFailureCycles)
                //    {
                //        _logger.LogError("GraphQL API unreachable for {MaxCycles} cycles. Stopping dispatcher gracefully.", _maxFailureCycles);
                //        break; // Stop dispatcher only
                //    }
                //}

                try
                {
                    await Task.Delay(_dispatchInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Dispatcher stopping gracefully (canceled).");
                    break;
                }
            }

            _logger.LogWarning("DataDispatcherWorker stopped after {FailureCount} consecutive failures. Scanner continues to run.", _failureCycleCount);
        }

        private async Task<bool> DispatchPendingEventsAsync(CancellationToken ct)
        {
            const string query = @"
SELECT * FROM ScanEvents 
WHERE Status = 'Pending' 
ORDER BY CreatedAt 
LIMIT 25;";

            var pendingEvents = await _repository.GetListAsync<ScanEventEntity>(query);
            if (pendingEvents == null || !pendingEvents.Any())
            {
                //_logger.LogDebug("No pending events found.");
                return true; // no work, not a failure
            }

            int successCount = 0;

            foreach (var ev in pendingEvents)
            {
                if (ct.IsCancellationRequested)
                    break;

                var success = await TrySendToGraphQLAsync(ev, ct);

                if (success)
                {
                    const string updateSql = @"
UPDATE ScanEvents
SET Status = 'Sent', SentAt = @SentAt, LastError = NULL
WHERE EventId = @EventId;";

                    await _repository.ExecuteAsync(updateSql, new { ev.EventId, SentAt = DateTimeOffset.UtcNow });
                    successCount++;
                }
                else
                {
                    const string failSql = @"
UPDATE ScanEvents
SET Attempts = @FailureCount,
    LastError = @Error,
    Status = 'Pending' 
WHERE EventId = @EventId;";

                    await _repository.ExecuteAsync(failSql, new
                    {
                        ev.EventId,
                        Error = "Failed to send to server",
                        @FailureCount=_failureCycleCount
                    });
                }
            }

            _logger.LogInformation("Dispatch batch complete. {Success}/{Total} sent successfully.", successCount, pendingEvents.Count());
            return successCount > 0; // ✅ If no successes, this counts as a failed cycle
        }

        private async Task<bool> TrySendToGraphQLAsync(ScanEventEntity ev, CancellationToken ct)
        {
            try
            {
                var gqlRequest = new
                {
                    query = @"
mutation ($input: ScanInput!) {
  sendScan(input: $input) {
    accepted
    message
    processedAt
  }
}",
                    variables = new
                    {
                        input = new
                        {
                            eventId = ev.EventId.ToString(),
                            code = ev.RawData,
                            deviceId = ev.DeviceId,
                            capturedAt = ev.Timestamp.ToString("O")
                        }
                    }
                };

                var json = JsonSerializer.Serialize(gqlRequest);
                var request = new HttpRequestMessage(HttpMethod.Post, _graphqlUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrWhiteSpace(_apiKey))
                    request.Headers.Add("x-api-key", _apiKey);

                var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GraphQL returned {StatusCode} for event {EventId}", response.StatusCode, ev.EventId);
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("errors", out _))
                {
                    _logger.LogWarning("GraphQL returned errors for event {EventId}: {Response}", ev.EventId, content);
                    return false;
                }

                var accepted = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("sendScan")
                    .GetProperty("accepted")
                    .GetBoolean();

                return accepted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event {EventId} to GraphQL", ev.EventId);
                return false;
            }
        }
    }
}
