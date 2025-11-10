using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Common.DTOs
{
    public class AppSettings
    {
    
        public int CommandTimeout { get; set; } = 60; // default seconds
        public string ServiceName { get; set; }
        public string ServiceBasePath { get; set; }

        public string GraphQLUrl { get; set; }
        public string GraphQLApiKey { get; set; }
        public string DeviceId { get; set; }
        public string WatchFolder { get; set; }
        public string ServiceDbConnection { get; set; }
        public string ScannerType { get; set; }
        public int DispatchMaxFailureCycles { get; set; }
        public int ConfigFetchMin { get; set; }

        public DeviceConfig DeviceConfig { get; set; }
    }

    public class GraphQLSettings
    {
        /// <summary>
        /// The full GraphQL endpoint URL (e.g. http://localhost:5181/graphql)
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// API key for authorization (sent as x-api-key header)
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

       
    }
}
