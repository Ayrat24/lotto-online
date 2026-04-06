using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MiniApp.Data;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260406103000_AddTicketStatus")]
    public partial class AddTicketStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "tickets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "AwaitingDraw");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_UserId_Status_DrawId",
                table: "tickets",
                columns: new[] { "UserId", "Status", "DrawId" });

            migrationBuilder.Sql(@"
UPDATE tickets AS t
SET ""Status"" = CASE
    WHEN d.""State"" <> 'Finished' OR d.""Numbers"" IS NULL OR btrim(d.""Numbers"") = '' THEN 'AwaitingDraw'
    WHEN (
        SELECT COUNT(*)
        FROM unnest(string_to_array(t.""Numbers"", ',')) AS tn(num)
        WHERE tn.num = ANY(string_to_array(d.""Numbers"", ','))
    ) >= 3 THEN 'WinningsAvailable'
    ELSE 'ExpiredNoWin'
END
FROM draws AS d
WHERE d.""Id"" = t.""DrawId"";
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tickets_UserId_Status_DrawId",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "tickets");
        }
    }
}



