using Dapper;
using DeviceHubMini.Common.Contracts;
using DeviceHubMini.Common.DTOs;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Model
{
    public class SqlLiteConnectionFactory : IConnectionFactory
    {
       
        private readonly AppSettings _appSettings;
        public SqlLiteConnectionFactory(AppSettings appSettings) => _appSettings = appSettings;

        public async Task<DbConnection> CreateConnectionAsync()
        {
            var connStr = _appSettings.ServiceDbConnection;
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException($"Connection string not found");
            var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);  
            return conn;
        }
        private static async Task EnsureSchemaAsync(DbConnection conn)
        {
            var sql = @"
                        CREATE TABLE IF NOT EXISTS ScanEvents (
                            EventId TEXT PRIMARY KEY,
                            RawData TEXT NOT NULL,
                            Timestamp TEXT NOT NULL,
                            DeviceId TEXT NOT NULL,
                            Status TEXT NOT NULL,
                            Attempts INTEGER NOT NULL DEFAULT 0,
                            LastError TEXT NULL,
                            CreatedAt TEXT NOT NULL,
                            LastTriedAt TEXT NULL,
                            SentAt TEXT NULL
                        );";
                      await conn.ExecuteAsync(sql);
        }
    }
}
