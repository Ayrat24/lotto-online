using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountedTicketOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discounted_ticket_offers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DrawId = table.Column<long>(type: "bigint", nullable: false),
                    NumberOfDiscountedTickets = table.Column<int>(type: "integer", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discounted_ticket_offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_discounted_ticket_offers_draws_DrawId",
                        column: x => x.DrawId,
                        principalTable: "draws",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_discounted_ticket_offers_DrawId_IsActive_UpdatedAtUtc",
                table: "discounted_ticket_offers",
                columns: new[] { "DrawId", "IsActive", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_discounted_ticket_offers_IsActive_UpdatedAtUtc",
                table: "discounted_ticket_offers",
                columns: new[] { "IsActive", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discounted_ticket_offers");
        }
    }
}
