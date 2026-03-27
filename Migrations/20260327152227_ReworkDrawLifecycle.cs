using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class ReworkDrawLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Numbers",
                table: "draws",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<decimal>(
                name: "PrizePool",
                table: "draws",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "draws",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "finished");

            migrationBuilder.Sql(@"
UPDATE draws
SET ""State"" = 'finished'
WHERE COALESCE(""State"", '') = '';

INSERT INTO draws (""Id"", ""Numbers"", ""CreatedAtUtc"", ""PrizePool"", ""State"")
SELECT
    missing.""DrawId"",
    NULL,
    missing.""CreatedAtUtc"",
    0,
    CASE
        WHEN missing.""DrawId"" = latest_missing.""DrawId"" THEN 'active'
        ELSE 'upcoming'
    END
FROM (
    SELECT t.""DrawId"", MAX(t.""PurchasedAtUtc"") AS ""CreatedAtUtc""
    FROM tickets t
    LEFT JOIN draws d ON d.""Id"" = t.""DrawId""
    WHERE d.""Id"" IS NULL
    GROUP BY t.""DrawId""
) AS missing
LEFT JOIN (
    SELECT MAX(t.""DrawId"") AS ""DrawId""
    FROM tickets t
    LEFT JOIN draws d ON d.""Id"" = t.""DrawId""
    WHERE d.""Id"" IS NULL
) AS latest_missing ON TRUE;
");

            migrationBuilder.CreateIndex(
                name: "IX_draws_State",
                table: "draws",
                column: "State");

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

            migrationBuilder.DropIndex(
                name: "IX_draws_State",
                table: "draws");

            migrationBuilder.DropColumn(
                name: "PrizePool",
                table: "draws");

            migrationBuilder.DropColumn(
                name: "State",
                table: "draws");

            migrationBuilder.Sql(@"
DELETE FROM draws
WHERE ""Numbers"" IS NULL;
");

            migrationBuilder.AlterColumn<string>(
                name: "Numbers",
                table: "draws",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
