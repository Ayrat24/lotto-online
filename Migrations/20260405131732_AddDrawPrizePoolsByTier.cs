using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDrawPrizePoolsByTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PrizePool",
                table: "draws",
                newName: "PrizePoolMatch5");

            migrationBuilder.AddColumn<decimal>(
                name: "PrizePoolMatch3",
                table: "draws",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrizePoolMatch4",
                table: "draws",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrizePoolMatch3",
                table: "draws");

            migrationBuilder.DropColumn(
                name: "PrizePoolMatch4",
                table: "draws");

            migrationBuilder.RenameColumn(
                name: "PrizePoolMatch5",
                table: "draws",
                newName: "PrizePool");
        }
    }
}
