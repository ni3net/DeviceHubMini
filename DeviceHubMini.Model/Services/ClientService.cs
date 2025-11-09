using DeviceHubMini.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeviceHubMini.Infrastructure.Services
{
    public class ClientService
    {
        private readonly HttpClient _http;
        private readonly AppSettings _appSettings;

        public ClientService(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<DeviceConfig?> GetDeviceConfigAsync(string deviceId, CancellationToken ct = default)
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

            var json = JsonSerializer.Serialize(gqlQuery);
            using var req = new HttpRequestMessage(HttpMethod.Post, _appSettings.GraphQLUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            req.Headers.Add("x-api-key", _appSettings.GraphQLApiKey);

            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();

            var response = await res.Content.ReadFromJsonAsync<GetConfigResponse>(cancellationToken: ct);
            return response?.Data?.GetConfig;
        }
    }
}
