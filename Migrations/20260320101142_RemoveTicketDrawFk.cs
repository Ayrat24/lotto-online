using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTicketDrawFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_draws_DrawId",
                table: "tickets");

            // Some environments already have draws.Id as a plain bigint (not identity).
            // Only drop identity if it exists.
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name = 'draws'
      AND column_name = 'Id'
      AND is_identity = 'YES'
  ) THEN
    EXECUTE 'ALTER TABLE draws ALTER COLUMN ""Id"" DROP IDENTITY';
  END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "draws",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_draws_DrawId",
                table: "tickets",
                column: "DrawId",
                principalTable: "draws",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
