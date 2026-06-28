using Microsoft.EntityFrameworkCore;

using System;

namespace NexusBank.Worker.Data
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Transactions")]
    public class DbTransaction
    {
        public int Id { get; set; }

        public string SenderAccountId { get; set; }

        public string ReceiverAccountId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public DateTime Timestamp { get; set; }
    }

    public class LedgerDbContext : DbContext
    {
        public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options) { }
        public DbSet<DbTransaction> Transactions => Set<DbTransaction>();



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DbTransaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasKey(e => e.Id); // 🌟 Ensure EF Core knows 'Id' is your primary key

                // Removed the broken manual mapping rules entirely!
            });
        }
    }

}