using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletAddressAndWithdrawalPayoutTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExternalPayoutCreatedAtUtc",
                table: "withdrawal_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPayoutId",
                table: "withdrawal_requests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPayoutState",
                table: "withdrawal_requests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WalletAddress",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_ExternalPayoutId",
                table: "withdrawal_requests",
                column: "ExternalPayoutId",
                unique: true,
                filter: "\"ExternalPayoutId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_withdrawal_requests_ExternalPayoutId",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "ExternalPayoutCreatedAtUtc",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "ExternalPayoutId",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "ExternalPayoutState",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "WalletAddress",
                table: "users");
        }
    }
}
