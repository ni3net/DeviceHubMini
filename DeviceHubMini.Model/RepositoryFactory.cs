using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Model
{
    public class RepositoryFactory 
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly AppSettings _appSettings;
        public RepositoryFactory(IConnectionFactory connectionFactory,AppSettings appSettings)
        {
            _connectionFactory = connectionFactory;
            _appSettings = appSettings;
        }

        //public IRepository Create(string connectionName)
        //{
        //    return new DapperRepository(_connectionFactory, connectionName, _appSettings);
        //}
    }
}
