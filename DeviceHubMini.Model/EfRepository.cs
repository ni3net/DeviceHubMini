
using Microsoft.EntityFrameworkCore;
using Model.Context;
using System.Data;
namespace DeviceHubMini.Model
{

    public class EfRepository : IRepository
    {
        private readonly AppDbContext _context;

        public EfRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetAsync<T>(string query, object? parameters = null) where T : class
        {
            // For EF, query should be LINQ instead of SQL.
            // Example: we interpret query as entity name (simplified)
            return await _context.Set<T>().FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> GetListAsync<T>(string query, object? parameters = null) where T : class
        {
            return await _context.Set<T>().ToListAsync();
        }

        public async Task<int> ExecuteAsync(string query, object? parameters = null)
        {
            // You can support raw SQL or SaveChanges
            return await _context.Database.ExecuteSqlRawAsync(query, parameters ?? Array.Empty<object>());
        }

        public Task<IDbConnection> GetConnection()
        {
            throw new NotImplementedException();
        }
    }

}
