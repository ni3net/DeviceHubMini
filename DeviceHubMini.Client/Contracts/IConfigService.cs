using DeviceHubMini.Client.GraphQL;

namespace DeviceHubMini.Client.Contracts
{
    public interface IConfigService
    {
        DeviceConfig GetConfig(string deviceId);
    }
}
