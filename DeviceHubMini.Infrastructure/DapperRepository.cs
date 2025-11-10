using Dapper;
using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Common.DTOs;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace DeviceHubMini.Model
{

    public class DapperRepository : IRepository
    {
        
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _defaultCommandTimeout;
        public IDbConnection _con;
        public DapperRepository(IConnectionFactory connectionFactory, AppSettings appSettings)
        {
            _connectionFactory = connectionFactory;
            _defaultCommandTimeout = appSettings.CommandTimeout;
        }

        public async Task<T?> GetAsync<T>(string query, object? parameters = null) where T : class
        {
            using var conn = await _connectionFactory.CreateConnectionAsync();

            return await conn.QueryFirstOrDefaultAsync<T>(query, parameters, commandTimeout: _defaultCommandTimeout);
        }

        public async Task<IEnumerable<T>> GetListAsync<T>(string query, object? parameters = null) where T : class
        {
            using var conn = await _connectionFactory.CreateConnectionAsync();
            return await conn.QueryAsync<T>(query, parameters, commandTimeout: _defaultCommandTimeout);
        }

        public async Task<int> ExecuteAsync(string query, object? parameters = null)
        {
            using var conn =  await _connectionFactory.CreateConnectionAsync();
            return await conn.ExecuteAsync(query, parameters, commandTimeout: _defaultCommandTimeout);
        }

        public async Task<IDbConnection> GetConnection()
        {
            return await _connectionFactory.CreateConnectionAsync();
          
        }
    }

}
