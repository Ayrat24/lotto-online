using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCryptoDepositsAndPaymentWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crypto_deposit_intents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderInvoiceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CheckoutLink = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastProviderEventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreditedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crypto_deposit_intents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crypto_deposit_intents_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_webhook_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DeliveryId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProviderObjectId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Error = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crypto_deposit_intents_CreatedAtUtc",
                table: "crypto_deposit_intents",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_crypto_deposit_intents_Provider_ProviderInvoiceId",
                table: "crypto_deposit_intents",
                columns: new[] { "Provider", "ProviderInvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crypto_deposit_intents_Status",
                table: "crypto_deposit_intents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_crypto_deposit_intents_UserId_CreatedAtUtc",
                table: "crypto_deposit_intents",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_events_Provider_DeliveryId",
                table: "payment_webhook_events",
                columns: new[] { "Provider", "DeliveryId" },
                unique: true,
                filter: "\"DeliveryId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_events_ReceivedAtUtc",
                table: "payment_webhook_events",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_events_Status",
                table: "payment_webhook_events",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crypto_deposit_intents");

            migrationBuilder.DropTable(
                name: "payment_webhook_events");
        }
    }
}
