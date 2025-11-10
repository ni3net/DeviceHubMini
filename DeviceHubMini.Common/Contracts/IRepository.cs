using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Common.Contracts
{
    public interface IRepository
    {
        Task<IDbConnection> GetConnection();
        Task<T?> GetAsync<T>(string query, object? parameters = null) where T : class;
        Task<IEnumerable<T>> GetListAsync<T>(string query, object? parameters = null) where T : class;
        Task<int> ExecuteAsync(string query, object? parameters = null);
    }

}
