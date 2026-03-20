using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDrawsAndBindTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DrawId",
                table: "tickets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "draws",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numbers = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draws", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_DrawId_PurchasedAtUtc",
                table: "tickets",
                columns: new[] { "DrawId", "PurchasedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_draws_CreatedAtUtc",
                table: "draws",
                column: "CreatedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_draws_DrawId",
                table: "tickets",
                column: "DrawId",
                principalTable: "draws",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_draws_DrawId",
                table: "tickets");

            migrationBuilder.DropTable(
                name: "draws");

            migrationBuilder.DropIndex(
                name: "IX_tickets_DrawId_PurchasedAtUtc",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "DrawId",
                table: "tickets");
        }
    }
}
