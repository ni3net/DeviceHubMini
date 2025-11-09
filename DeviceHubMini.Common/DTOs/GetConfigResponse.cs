using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Common.DTOs
{
    public class GetConfigResponse
    {
        public GetConfigData Data { get; set; }
    }
    public class GetConfigData
    {
        public DeviceConfig GetConfig { get; set; } = new();
    }

    public class DeviceConfig
    {
        public int DebounceMs { get; set; }
        public bool BatchingEnabled { get; set; }
        public int DispatchIntervalMs { get; set; }
    }
}
