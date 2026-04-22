using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    public partial class AddDrawPurchaseClosesAtUtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PurchaseClosesAtUtc",
                table: "draws",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE draws
                SET "PurchaseClosesAtUtc" = COALESCE("PurchaseClosesAtUtc", "CreatedAtUtc" + INTERVAL '1 hour')
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PurchaseClosesAtUtc",
                table: "draws",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_draws_PurchaseClosesAtUtc",
                table: "draws",
                column: "PurchaseClosesAtUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_draws_PurchaseClosesAtUtc",
                table: "draws");

            migrationBuilder.DropColumn(
                name: "PurchaseClosesAtUtc",
                table: "draws");
        }
    }
}

