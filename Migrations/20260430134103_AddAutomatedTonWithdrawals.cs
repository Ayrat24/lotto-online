using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomatedTonWithdrawals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AssetAmount",
                table: "withdrawal_requests",
                type: "numeric(18,9)",
                precision: 18,
                scale: 9,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AssetRate",
                table: "withdrawal_requests",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayoutAttemptCount",
                table: "withdrawal_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PayoutConfirmedAtUtc",
                table: "withdrawal_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PayoutLastAttemptAtUtc",
                table: "withdrawal_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutLastError",
                table: "withdrawal_requests",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutMemo",
                table: "withdrawal_requests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayoutSeqno",
                table: "withdrawal_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PayoutSubmittedAtUtc",
                table: "withdrawal_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_requests_AssetCode_ExternalPayoutState_CreatedAt~",
                table: "withdrawal_requests",
                columns: new[] { "AssetCode", "ExternalPayoutState", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_withdrawal_requests_AssetCode_ExternalPayoutState_CreatedAt~",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "AssetAmount",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "AssetRate",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "PayoutAttemptCount",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "PayoutConfirmedAtUtc",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "PayoutLastAttemptAtUtc",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "PayoutLastError",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "PayoutMemo",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "PayoutSeqno",
                table: "withdrawal_requests");

            migrationBuilder.DropColumn(
                name: "PayoutSubmittedAtUtc",
                table: "withdrawal_requests");
        }
    }
}
