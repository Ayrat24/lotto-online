using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDrawCardColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardColor",
                table: "draws",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "gold");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardColor",
                table: "draws");
        }
    }
}
