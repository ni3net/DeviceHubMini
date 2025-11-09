using DeviceHubMini.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Infrastructure.Context
{
    public class DeviceHubDbContext : DbContext
    {
        private readonly string _dbPath;
        public DbSet<ScanEventEntity> ScanEvents => Set<ScanEventEntity>();

        public DeviceHubDbContext(string dbPath) => _dbPath = dbPath;

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={_dbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScanEventEntity>()
                .HasKey(x => x.EventId);
            modelBuilder.Entity<ScanEventEntity>()
                .HasIndex(x => x.Status);
            modelBuilder.Entity<ScanEventEntity>()
                .Property(x => x.Status).HasMaxLength(16);
        }
    }
}
