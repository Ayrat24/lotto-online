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

