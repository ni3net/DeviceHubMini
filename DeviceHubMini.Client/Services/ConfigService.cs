using DeviceHubMini.Client.Contracts;
using DeviceHubMini.Client.GraphQL;

namespace DeviceHubMini.Client.Services
{
    public class ConfigService : IConfigService
    {
        private readonly IConfiguration _cfg;
        public ConfigService(IConfiguration cfg) => _cfg = cfg;

        public DeviceConfig GetConfig(string deviceId)
        {
            // In real life you could look up per-device settings here.
            int debounce = _cfg.GetValue("DeviceDefaults:DebounceMs", 500);
            bool batching = _cfg.GetValue("DeviceDefaults:BatchingEnabled", false);
            int dispatch = _cfg.GetValue("DeviceDefaults:DispatchIntervalMs", 5000);
            return new DeviceConfig(debounce, batching, dispatch);
        }
    }
}
