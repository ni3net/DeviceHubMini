using DeviceHubMini.Common.DTOs;
using DeviceHubMini.Infrastructure.Contracts;
using DeviceHubMini.Infrastructure.Entities;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHubMini.Infrastructure.Services
{
    /// <summary>
    /// Handles all communication with the GraphQL backend � 
    /// includes both configuration fetch and event submission.
    /// </summary>
    public sealed class GraphQLClientService : IGraphQLClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GraphQLClientService> _logger;
        //private readonly string _graphqlUrl;
        //private readonly string _apiKey;

        public GraphQLClientService(
            HttpClient httpClient,
            ILogger<GraphQLClientService> logger,
            AppSettings appSettings)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Sends a scan event to the GraphQL API via the "sendScan" mutation.
        /// </summary>
        [Trace]
        [MethodImpl(MethodImplOptions.NoInlining)]

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

                using var content = new StringContent(
                    JsonSerializer.Serialize(gqlRequest),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.PostAsync("", content, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GraphQL returned {StatusCode} for event {EventId}", response.StatusCode, ev.EventId);
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(responseString);

                if (doc.RootElement.TryGetProperty("errors", out _))
                {
                    _logger.LogWarning("GraphQL returned errors for event {EventId}", ev.EventId);
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
                //  _logger.LogError(ex, "Error sending event {EventId} to GraphQL", ev.EventId);
                _logger.LogError("Error sending event {EventId} to GraphQL", ev.EventId);
                return false;
            }
        }

        /// <summary>
        /// Fetches the device configuration from GraphQL using "getConfig" query.
        /// </summary>
        [Trace]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<DeviceConfig?> GetDeviceConfigAsync(string deviceId, CancellationToken ct = default)
        {
            try
            {
                var gqlQuery = new
                {
                    query = @"
query ($deviceId: String!) {
  getConfig(deviceId: $deviceId) {
    debounceMs
    batchingEnabled
    dispatchIntervalMs
  }
}",
                    variables = new { deviceId }
                };

                using var content = new StringContent(
                    JsonSerializer.Serialize(gqlQuery),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.PostAsync("", content, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch config for {DeviceId}. Status: {StatusCode}", deviceId, response.StatusCode);
                    return null;
                }

                var gqlResponse = await response.Content.ReadFromJsonAsync<GetConfigResponse>(cancellationToken: ct);
                return gqlResponse?.Data?.GetConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching configuration for {DeviceId}", deviceId);
                //_logger.LogError(ex, "Error fetching configuration for {DeviceId}", deviceId);
                return null;
            }
        }
    }
}
