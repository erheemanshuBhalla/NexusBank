using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NexusBank.Api.Migrations
{
    public partial class InitialBankSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderAccountId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiverAccountId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            // Seed our default ledger test profiles
            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "Balance", "Currency", "OwnerName" },
                values: new object[,]
                {
                    { "ACC-SENDER-111", 5000.00m, "USD", "Alice Smith" },
                    { "ACC-RECEIVER-222", 150.50m, "USD", "Bob Jones" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Accounts");
            migrationBuilder.DropTable(name: "Transactions");
        }
    }
}