using Dapper;
using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Common.DTOs;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DeviceHubMini.Model
{
    /// <summary>
    /// Generic Dapper-based repository for executing SQL commands and queries.
    /// </summary>
    public class DapperRepository : IRepository
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _defaultCommandTimeout;

        public DapperRepository(IConnectionFactory connectionFactory, AppSettings appSettings)
        {
            _connectionFactory = connectionFactory;
            _defaultCommandTimeout = appSettings?.CommandTimeout > 0 ? appSettings.CommandTimeout : 30;
        }

        public DapperRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _defaultCommandTimeout = 30;
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
            using var conn = await _connectionFactory.CreateConnectionAsync();
            return await conn.ExecuteAsync(query, parameters, commandTimeout: _defaultCommandTimeout);
        }

        /// <summary>
        /// Opens a reusable connection (for batch operations or transactions).
        /// </summary>
        public async Task<IDbConnection> GetConnection()
        {
            return await _connectionFactory.CreateConnectionAsync();
        }

        /// <summary>
        /// Executes a command within a transaction.
        /// </summary>
        public async Task<int> ExecuteTransactionAsync(string query, object? parameters = null)
        {
            using var conn = await _connectionFactory.CreateConnectionAsync();
            using var transaction = conn.BeginTransaction();
            try
            {
                var result = await conn.ExecuteAsync(query, parameters, transaction, commandTimeout: _defaultCommandTimeout);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
