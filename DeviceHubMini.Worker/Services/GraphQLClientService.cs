using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DeviceHubMini.Infrastructure.Entities;
using DeviceHubMini.Model;
using Microsoft.Extensions.Logging;

namespace DeviceHubMini.Services.GraphQL
{
    public sealed class GraphQLClientService : IGraphQLClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GraphQLClientService> _logger;
        private readonly string _graphqlUrl;
        private readonly string _apiKey;

        public GraphQLClientService(
            IHttpClientFactory httpClientFactory,
            ILogger<GraphQLClientService> logger,
            AppSettings appSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _graphqlUrl = appSettings.GraphQLUrl;
            _apiKey = appSettings.GraphQLApiKey;
        }

        public async Task<bool> SendScanEventAsync(ScanEventEntity ev, CancellationToken ct)
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

                var client = _httpClientFactory.CreateClient(nameof(GraphQLClientService));
                var req = new HttpRequestMessage(HttpMethod.Post, _graphqlUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(gqlRequest), Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrWhiteSpace(_apiKey))
                    req.Headers.Add("x-api-key", _apiKey);

                var res = await client.SendAsync(req, ct);

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GraphQL returned {StatusCode} for event {EventId}", res.StatusCode, ev.EventId);
                    return false;
                }

                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
                if (doc.RootElement.TryGetProperty("errors", out _))
                {
                    _logger.LogWarning("GraphQL returned errors for {EventId}.", ev.EventId);
                    return false;
                }

                return doc.RootElement
                    .GetProperty("data")
                    .GetProperty("sendScan")
                    .GetProperty("accepted")
                    .GetBoolean();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event {EventId} to GraphQL", ev.EventId);
                return false;
            }
        }
    }
}
