using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "withdrawal_requests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReviewedByAdmin = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_withdrawal_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_withdrawal_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_CreatedAtUtc",
                table: "withdrawal_requests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_Status",
                table: "withdrawal_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_Status_CreatedAtUtc",
                table: "withdrawal_requests",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_UserId_CreatedAtUtc",
                table: "withdrawal_requests",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "withdrawal_requests");
        }
    }
}
