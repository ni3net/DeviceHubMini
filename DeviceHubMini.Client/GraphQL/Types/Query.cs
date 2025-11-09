using DeviceHubMini.Client.Contracts;
using DeviceHubMini.Client.Services;

namespace DeviceHubMini.Client.GraphQL.Types
{
    public class Query
    {
        private readonly ILogger<Query> _logger; 
        private readonly IConfigService _configService;


        public Query(IConfigService configService, ILogger<Query> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        [GraphQLName("getConfig")]
        public DeviceConfig GetConfi(string deviceId)
        {
            _logger.LogInformation("Fetching config for {DeviceId}", deviceId);
            return _configService.GetConfig(deviceId);
        }
    }
}
