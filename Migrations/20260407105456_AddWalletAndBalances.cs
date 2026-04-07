using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletAndBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TicketCost",
                table: "draws",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 2m);

            migrationBuilder.CreateTable(
                name: "server_wallet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_wallet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserDelta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UserBalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ServerDelta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ServerBalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallet_transactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_CreatedAtUtc",
                table: "wallet_transactions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_Type",
                table: "wallet_transactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_UserId_CreatedAtUtc",
                table: "wallet_transactions",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.InsertData(
                table: "server_wallet",
                columns: new[] { "Id", "Balance", "UpdatedAtUtc" },
                values: new object[] { 1, 0m, DateTimeOffset.UtcNow });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server_wallet");

            migrationBuilder.DropTable(
                name: "wallet_transactions");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TicketCost",
                table: "draws");
        }
    }
}
