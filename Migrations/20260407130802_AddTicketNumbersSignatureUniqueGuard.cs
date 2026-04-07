using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketNumbersSignatureUniqueGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumbersSignature",
                table: "tickets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_UserId_DrawId_NumbersSignature",
                table: "tickets",
                columns: new[] { "UserId", "DrawId", "NumbersSignature" },
                unique: true,
                filter: "\"NumbersSignature\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tickets_UserId_DrawId_NumbersSignature",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "NumbersSignature",
                table: "tickets");
        }
    }
}
