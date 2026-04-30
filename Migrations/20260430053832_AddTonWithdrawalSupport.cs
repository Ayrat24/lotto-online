using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTonWithdrawalSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetCode",
                table: "withdrawal_requests",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "BTC");

            migrationBuilder.AddColumn<string>(
                name: "TonWalletAddress",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetCode",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "TonWalletAddress",
                table: "users");
        }
    }
}
