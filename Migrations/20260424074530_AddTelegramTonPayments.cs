using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramTonPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternativeCheckoutLink",
                table: "crypto_deposit_intents",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AssetAmount",
                table: "crypto_deposit_intents",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetCode",
                table: "crypto_deposit_intents",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationAddress",
                table: "crypto_deposit_intents",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationMemo",
                table: "crypto_deposit_intents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Network",
                table: "crypto_deposit_intents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "crypto_deposit_intents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "btcpay_crypto");

            migrationBuilder.AddColumn<string>(
                name: "ProviderTransactionId",
                table: "crypto_deposit_intents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlternativeCheckoutLink",
                table: "crypto_deposit_intents");

            migrationBuilder.DropColumn(
                name: "AssetAmount",
                table: "crypto_deposit_intents");

            migrationBuilder.DropColumn(
                name: "AssetCode",
                table: "crypto_deposit_intents");

            migrationBuilder.DropColumn(
                name: "DestinationAddress",
                table: "crypto_deposit_intents");

            migrationBuilder.DropColumn(
                name: "DestinationMemo",
                table: "crypto_deposit_intents");

            migrationBuilder.DropColumn(
                name: "Network",
                table: "crypto_deposit_intents");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "crypto_deposit_intents");

            migrationBuilder.DropColumn(
                name: "ProviderTransactionId",
                table: "crypto_deposit_intents");
        }
    }
}
