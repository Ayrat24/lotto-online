namespace MiniApp.Features.Payments;

public sealed record CreateCryptoDepositRequest(string InitData, decimal Amount, string? Currency, string? PaymentMethod);

public sealed record PaymentSystemsView(bool Enabled, string? DefaultPaymentMethod, IReadOnlyList<PaymentSystemView> Systems, string? TonConnectTwaReturnUrl = null);

public sealed record PaymentSystemView(
    string Key,
    string Provider,
    string Kind,
    bool SupportsNativeTelegram,
    bool SupportsAlternativeLink,
    string? AssetCode,
    string? Network);

public sealed record GetCryptoDepositStatusRequest(string InitData, long DepositId);

public sealed record CreateCryptoDepositResult(bool Success, string? Error, CryptoDepositView? Deposit = null);

public sealed record CryptoDepositStatusResult(bool Success, string? Error, CryptoDepositView? Deposit = null);

public sealed record ProcessWebhookResult(bool Success, string? Error, bool Duplicate = false);

public sealed record TelegramTonAdminDepositDiagnosticsResult(
    bool Success,
    string? Error,
    IReadOnlyList<TelegramTonAdminDepositDiagnosticView> Deposits);

public sealed record TelegramTonAdminDepositReconcileResult(
    bool Success,
    string? Error,
    TelegramTonAdminDepositDiagnosticView? Deposit = null,
    bool Changed = false);

public sealed record CryptoDepositView(
    long Id,
    string PaymentMethod,
    string Provider,
    string ProviderInvoiceId,
    decimal Amount,
    string Currency,
    decimal? AssetAmount,
    string? AssetCode,
    string? Network,
    string Status,
    string CheckoutLink,
    string? AlternativeCheckoutLink,
    string? DestinationAddress,
    string? DestinationMemo,
    string? ProviderTransactionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CreditedAtUtc);

public sealed record TelegramTonAdminDepositDiagnosticView(
    long Id,
    long UserId,
    long TelegramUserId,
    decimal Amount,
    string Currency,
    decimal? ExpectedTonAmount,
    string Status,
    string ProviderInvoiceId,
    string? DestinationAddress,
    string? DestinationMemo,
    string? ProviderTransactionId,
    string? LastProviderEventType,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CreditedAtUtc,
    string CreditReference,
    bool WalletTransactionExists,
    bool LookupAttempted,
    bool LookupSuccess,
    bool LookupTransferFound,
    string? LookupError,
    string? LookupTransactionId,
    decimal? LookupReceivedTonAmount,
    DateTimeOffset? LookupObservedAtUtc,
    string? LookupExplorerLink,
    string? LookupSenderAddress,
    bool RecentWalletTransfersAttempted,
    int RecentWalletRawTransactionCount,
    string? RecentWalletTransfersError,
    IReadOnlyList<TelegramTonAdminIncomingTransferDiagnosticView> RecentWalletTransfers);

public sealed record TelegramTonAdminIncomingTransferDiagnosticView(
    string? TransactionId,
    decimal? ReceivedTonAmount,
    DateTimeOffset? ObservedAtUtc,
    string? ExplorerLink,
    string? SenderAddress,
    string? Memo,
    bool MemoMatches,
    bool AmountMatches,
    bool TimeMatches);

public sealed record TonConnectRequestContextView(
    string Scheme,
    string Host,
    string PathBase,
    string? ForwardedProto,
    string? ForwardedHost);

public sealed record TonConnectResolvedUrlsView(
    string SiteRoot,
    string AppUrl,
    string RootManifestUrl,
    string AppManifestUrl);

public sealed record TonConnectManifestView(
    string? Url,
    string? Name,
    string? IconUrl,
    string? TermsOfUseUrl,
    string? PrivacyPolicyUrl);

public sealed record TonConnectProbeView(
    string Name,
    string Url,
    bool Ok,
    int? StatusCode,
    string? ContentType,
    string? RedirectLocation,
    string? FinalUrl,
    bool IsHttps,
    long? ContentLength,
    string? Error,
    string? BodySnippet);

public sealed record TonConnectDiagnosticsView(
    DateTimeOffset GeneratedAtUtc,
    string? ConfiguredBotWebAppUrl,
    string? ConfiguredTonConnectTwaReturnUrl,
    TonConnectRequestContextView Request,
    TonConnectResolvedUrlsView ResolvedUrls,
    IReadOnlyList<string> Issues,
    TonConnectManifestView? RootManifest,
    TonConnectManifestView? AppManifest,
    IReadOnlyList<TonConnectProbeView> Probes);

public sealed record TonWithdrawalWorkerDiagnosticsView(
    DateTimeOffset GeneratedAtUtc,
    bool PaymentsEnabled,
    bool TelegramTonEnabled,
    bool ServerWithdrawalsEnabled,
    string ConfiguredNetwork,
    string ApiBaseUrl,
    TelegramTonHotWalletStateResult HotWallet,
    TonWithdrawalWorkerRuntimeView Worker,
    TonWithdrawalQueueDiagnosticsView Queue,
    IReadOnlyList<string> Issues,
    int QueuedCount,
    int RetryPendingCount,
    int SendingCount,
    int SubmittedCount,
    int ConfirmedCount,
    IReadOnlyList<TonWithdrawalRequestDiagnosticView> RecentRequests);

public sealed record TonWithdrawalWorkerRuntimeView(
    bool Started,
    bool DisabledByConfiguration,
    string? DisabledReason,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? LastCycleStartedAtUtc,
    DateTimeOffset? LastCycleCompletedAtUtc,
    DateTimeOffset? LastProgressAtUtc,
    long CycleCount,
    int? LastChangedCount,
    string? LastError,
    double? IntervalSeconds);

public sealed record TonWithdrawalQueueDiagnosticsView(
    bool CanProcessQueuedWithdrawals,
    IReadOnlyList<string> BlockingReasons,
    TonWithdrawalRequestDiagnosticView? OldestQueuedRequest,
    double? OldestQueuedAgeMinutes);

public sealed record TonWithdrawalRequestDiagnosticView(
    long Id,
    long UserId,
    long TelegramUserId,
    decimal AmountUsd,
    decimal? AmountTon,
    string PayoutAddress,
    string Status,
    string ReviewStatus,
    string? RawPayoutState,
    string? ExternalPayoutId,
    string? Memo,
    int AttemptCount,
    int? Seqno,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExternalPayoutCreatedAtUtc,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    string? LastError,
    IReadOnlyList<string> Issues);

