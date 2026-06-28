using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NexusBank.Api.Data;

namespace NexusBank.Api.Migrations
{
    [DbContext(typeof(BankDbContext))]
    partial class BankDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.27")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("NexusBank.Api.Data.Account", b =>
            {
                b.Property<string>("Id").HasColumnType("nvarchar(450)");
                b.Property<decimal>("Balance").HasColumnType("decimal(18,2)");
                b.Property<string>("Currency").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("OwnerName").IsRequired().HasColumnType("nvarchar(max)");
                b.HasKey("Id");
                b.ToTable("Accounts");
            });

            modelBuilder.Entity("NexusBank.Api.Data.LedgerTransaction", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
                b.Property<decimal>("Amount").HasColumnType("decimal(18,2)");
                b.Property<string>("Currency").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("ReceiverAccountId").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("SenderAccountId").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<DateTime>("Timestamp").HasColumnType("datetime2");
                b.HasKey("Id");
                b.ToTable("Transactions");
            });
        }
    }
}