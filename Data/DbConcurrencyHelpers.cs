using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MiniApp.Data;

/// <summary>
/// Helpers for recognizing optimistic-concurrency conflicts and the wallet idempotency
/// unique-violation, so money operations can fail safely instead of double-spending.
/// </summary>
internal static class DbConcurrencyHelpers
{
    public const string WalletIdempotencyIndexName = "IX_wallet_transactions_Idempotent_Type_Reference";

    public static bool IsWalletIdempotencyViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg
           && pg.SqlState == PostgresErrorCodes.UniqueViolation
           && string.Equals(pg.ConstraintName, WalletIdempotencyIndexName, StringComparison.Ordinal);

    public static bool IsConcurrencyOrIdempotencyConflict(Exception ex)
        => ex is DbUpdateConcurrencyException
           || (ex is DbUpdateException ue && IsWalletIdempotencyViolation(ue));
}
