using DeviceHubMini.Client;
using DeviceHubMini.Client.Contracts;
using DeviceHubMini.Client.GraphQL;
using DeviceHubMini.Client.GraphQL.Types;
using DeviceHubMini.Client.Services;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

public class GraphQLTests
{
    private readonly IRequestExecutor _executor;

    public GraphQLTests()
    {
        _executor = new ServiceCollection()
      .AddScoped<DeviceHubMini.Client.GraphQL.Types.Query>()
      .AddLogging()
      .AddScoped<IConfigService, FakeConfigService>() // optional: mock or fake
      .AddScoped<IEventStore, InMemoryEventStore>()
      .AddGraphQLServer()
      .AddQueryType<Query>()
      .AddMutationType<Mutation>()
      .BuildRequestExecutorAsync()
      .GetAwaiter()
      .GetResult();
    }

    [Fact]
    public async Task GetConfig_Should_Return_DeviceConfig()
    {
        // Arrange
        string query = @"{
      getConfig(deviceId: ""D1"") {
        debounceMs
        batchingEnabled
        dispatchIntervalMs
      }
    }";

        // Act
        IExecutionResult result = await _executor.ExecuteAsync(query);

        var json = result.ToJson();
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(json.Contains("errors"), $"Unexpected GraphQL errors: {json}");
        Assert.True(doc.RootElement.GetProperty("data").TryGetProperty("getConfig", out _), "Missing getConfig property in GraphQL response");
    }


    [Fact]
    public async Task SendScan_Should_Return_Accepted()
    {
        // Arrange
        string mutation = @"
mutation {
  sendScan(input: { eventId: ""123"", code: ""ABC123"", deviceId: ""D1"", capturedAt: ""2025-11-09T12:00:00Z"" }) {
    accepted
    message
    processedAt
  }
}";

        // Act
        IExecutionResult result = await _executor.ExecuteAsync(mutation);

        var json = result.ToJson();
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.False(json.Contains("errors"), $"Unexpected GraphQL errors: {json}");
        var sendScan = doc.RootElement.GetProperty("data").GetProperty("sendScan");
        Assert.True(sendScan.GetProperty("accepted").GetBoolean());
    }


}
public class FakeConfigService : IConfigService
{
    public DeviceConfig GetConfig(string deviceId) =>
        new DeviceConfig(DebounceMs: 500, BatchingEnabled: false, DispatchIntervalMs: 5000);
}