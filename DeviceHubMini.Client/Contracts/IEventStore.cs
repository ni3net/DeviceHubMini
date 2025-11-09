namespace DeviceHubMini.Client.Contracts
{
    public interface IEventStore
    {
        bool IsDuplicate(string eventId);
        void MarkProcessed(string eventId);
        long TotalReceived { get; }
        long TotalDuplicates { get; }
    }
}
