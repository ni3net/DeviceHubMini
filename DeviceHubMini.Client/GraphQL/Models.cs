namespace DeviceHubMini.Client.GraphQL
{

    public record DeviceConfig(int DebounceMs, bool BatchingEnabled, int DispatchIntervalMs);

    public record ScanInput(string EventId, string Code, string DeviceId, DateTimeOffset CapturedAt);

    public record ScanResult(bool Accepted, string? Message, DateTimeOffset ProcessedAt);
}
