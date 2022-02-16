using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace KN.SafeCommunicationPlatform.EF
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<SendingData> SendingData { get; set; } = default!;

        public DbSet<ReceiveData> ReceivaData { get; set; } = default!;
    }
}