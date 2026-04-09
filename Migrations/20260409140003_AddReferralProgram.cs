using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReferredAtUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReferredByUserId",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "referral_program_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    InviterBonusAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InviteeBonusAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinQualifyingDepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EligibilityWindowDays = table.Column<int>(type: "integer", nullable: false),
                    MonthlyInviterBonusCap = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByAdmin = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_program_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "referral_rewards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InviterUserId = table.Column<long>(type: "bigint", nullable: false),
                    InviteeUserId = table.Column<long>(type: "bigint", nullable: false),
                    RecipientUserId = table.Column<long>(type: "bigint", nullable: false),
                    DepositIntentId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_rewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_referral_rewards_crypto_deposit_intents_DepositIntentId",
                        column: x => x.DepositIntentId,
                        principalTable: "crypto_deposit_intents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_referral_rewards_users_InviteeUserId",
                        column: x => x.InviteeUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_referral_rewards_users_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_referral_rewards_users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_InviteCode",
                table: "users",
                column: "InviteCode",
                unique: true,
                filter: "\"InviteCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_ReferredByUserId",
                table: "users",
                column: "ReferredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_referral_rewards_DepositIntentId_Type",
                table: "referral_rewards",
                columns: new[] { "DepositIntentId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referral_rewards_InviteeUserId",
                table: "referral_rewards",
                column: "InviteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_referral_rewards_InviterUserId_CreatedAtUtc",
                table: "referral_rewards",
                columns: new[] { "InviterUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_referral_rewards_RecipientUserId_Type_CreatedAtUtc",
                table: "referral_rewards",
                columns: new[] { "RecipientUserId", "Type", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_ReferredByUserId",
                table: "users",
                column: "ReferredByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_ReferredByUserId",
                table: "users");

            migrationBuilder.DropTable(
                name: "referral_program_settings");

            migrationBuilder.DropTable(
                name: "referral_rewards");

            migrationBuilder.DropIndex(
                name: "IX_users_InviteCode",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_ReferredByUserId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ReferredAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ReferredByUserId",
                table: "users");
        }
    }
}
