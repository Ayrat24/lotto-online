using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBalanceConcurrencyAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: "xmin" is a PostgreSQL system column that already exists on every table
            // and is mapped as an optimistic-concurrency token in the model. No DDL is emitted
            // for it (attempting to ADD a column named "xmin" would fail with
            // "column name \"xmin\" conflicts with a system column name").

            // Database-level idempotency backstop for crediting / claiming / refunding so a
            // concurrent double-write hits a unique violation instead of paying out twice.
            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_Idempotent_Type_Reference",
                table: "wallet_transactions",
                columns: new[] { "Type", "Reference" },
                unique: true,
                filter: "\"Reference\" IS NOT NULL AND \"Type\" IN ('CryptoDepositCredited', 'WinningsClaimed', 'WithdrawalConfirmed', 'WithdrawalDeniedRefund', 'ReferralInviterBonus', 'ReferralInviteeBonus')");

            // Hard backstop so no code path can ever persist a negative balance.
            migrationBuilder.Sql(
                "ALTER TABLE users ADD CONSTRAINT \"CK_users_Balance_NonNegative\" CHECK (\"Balance\" >= 0);");
            migrationBuilder.Sql(
                "ALTER TABLE server_wallet ADD CONSTRAINT \"CK_server_wallet_Balance_NonNegative\" CHECK (\"Balance\" >= 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE server_wallet DROP CONSTRAINT IF EXISTS \"CK_server_wallet_Balance_NonNegative\";");
            migrationBuilder.Sql("ALTER TABLE users DROP CONSTRAINT IF EXISTS \"CK_users_Balance_NonNegative\";");

            migrationBuilder.DropIndex(
                name: "IX_wallet_transactions_Idempotent_Type_Reference",
                table: "wallet_transactions");
        }
    }
}
