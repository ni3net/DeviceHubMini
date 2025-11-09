using DeviceHubMini.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Common.Contracts
{
    public interface IRepositoryFactory
    {
        IRepository Create(string connectionName);
    }
}
