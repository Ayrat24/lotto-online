using Microsoft.Extensions.Options;

namespace MiniApp.Features.Payments;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    public bool Enabled { get; set; }

    public string DefaultPaymentMethod { get; set; } = PaymentMethodKeys.TelegramTon;

    public BtcPayOptions BtcPay { get; set; } = new();

    public TelegramTonOptions TelegramTon { get; set; } = new();

    public PaymentsOpsOptions Ops { get; set; } = new();
}

public static class PaymentMethodKeys
{
    public const string BtcPayCrypto = "btcpay_crypto";
    public const string TelegramTon = "telegram_ton";
}

public sealed class BtcPayOptions
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public string StoreId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string WithdrawalsPullPaymentId { get; set; } = string.Empty;

    public string WithdrawalsPaymentMethod { get; set; } = "BTC-CHAIN";

    public string WebhookSecret { get; set; } = string.Empty;

    public string DefaultCurrency { get; set; } = "USD";

    public int RequestTimeoutSeconds { get; set; } = 15;

    public int MaxRetryAttempts { get; set; } = 3;
}

public sealed class TelegramTonOptions
{
    public bool Enabled { get; set; }

    public bool AutoRefreshEnabled { get; set; } = true;

    public string MerchantAddress { get; set; } = string.Empty;

    public string MerchantName { get; set; } = "Lotto";

    public decimal UsdPerTon { get; set; }

    public string RateApiBaseUrl { get; set; } = "https://api.coingecko.com/api/v3/";

    public string RateApiKey { get; set; } = string.Empty;

    public int RateRefreshIntervalMinutes { get; set; } = 5;

    public int MaxRateAgeMinutes { get; set; } = 30;

    public string ApiBaseUrl { get; set; } = "https://toncenter.com/api/v2/";

    public string ApiKey { get; set; } = string.Empty;

    public int RequestTimeoutSeconds { get; set; } = 15;

    public int TransactionSearchLimit { get; set; } = 25;

    public int PaymentTimeoutMinutes { get; set; } = 20;

    public string ExplorerBaseUrl { get; set; } = "https://tonviewer.com/transaction/";
}

public sealed class PaymentsOpsOptions
{
    public bool EnableReconciliation { get; set; }
}

public sealed class PaymentsOptionsValidator : IValidateOptions<PaymentsOptions>
{
    public ValidateOptionsResult Validate(string? name, PaymentsOptions options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var errors = new List<string>();
        var btcPay = options.BtcPay ?? new BtcPayOptions();
        var telegramTon = options.TelegramTon ?? new TelegramTonOptions();
        var defaultPaymentMethod = (options.DefaultPaymentMethod ?? string.Empty).Trim().ToLowerInvariant();

        if (defaultPaymentMethod.Length > 0
            && defaultPaymentMethod != PaymentMethodKeys.BtcPayCrypto
            && defaultPaymentMethod != PaymentMethodKeys.TelegramTon)
        {
            errors.Add("Payments:DefaultPaymentMethod must be btcpay_crypto or telegram_ton.");
        }

        if (!btcPay.Enabled && !telegramTon.Enabled)
            errors.Add("Enable at least one payment provider when Payments:Enabled=true.");

        if (btcPay.Enabled)
        {
            if (string.IsNullOrWhiteSpace(btcPay.BaseUrl))
                errors.Add("Payments:BtcPay:BaseUrl is required when BTCPay is enabled.");
            else if (!Uri.TryCreate(btcPay.BaseUrl, UriKind.Absolute, out _))
                errors.Add("Payments:BtcPay:BaseUrl must be an absolute URI.");

            if (string.IsNullOrWhiteSpace(btcPay.StoreId))
                errors.Add("Payments:BtcPay:StoreId is required when BTCPay is enabled.");

            if (string.IsNullOrWhiteSpace(btcPay.ApiKey))
                errors.Add("Payments:BtcPay:ApiKey is required when BTCPay is enabled.");

            if (string.IsNullOrWhiteSpace(btcPay.WithdrawalsPullPaymentId))
                errors.Add("Payments:BtcPay:WithdrawalsPullPaymentId is required when BTCPay is enabled.");

            if (string.IsNullOrWhiteSpace(btcPay.WithdrawalsPaymentMethod))
                errors.Add("Payments:BtcPay:WithdrawalsPaymentMethod is required when BTCPay is enabled.");

            if (btcPay.RequestTimeoutSeconds <= 0)
                errors.Add("Payments:BtcPay:RequestTimeoutSeconds must be greater than 0.");

            if (btcPay.MaxRetryAttempts <= 0)
                errors.Add("Payments:BtcPay:MaxRetryAttempts must be greater than 0.");
        }

        if (telegramTon.Enabled)
        {
            if (string.IsNullOrWhiteSpace(telegramTon.MerchantAddress))
                errors.Add("Payments:TelegramTon:MerchantAddress is required when Telegram TON is enabled.");

            if (telegramTon.UsdPerTon <= 0m)
                errors.Add("Payments:TelegramTon:UsdPerTon must be greater than 0 when Telegram TON is enabled.");

            if (telegramTon.AutoRefreshEnabled)
            {
                if (!string.IsNullOrWhiteSpace(telegramTon.RateApiBaseUrl)
                    && !Uri.TryCreate(telegramTon.RateApiBaseUrl, UriKind.Absolute, out _))
                {
                    errors.Add("Payments:TelegramTon:RateApiBaseUrl must be an absolute URI.");
                }

                if (telegramTon.RateRefreshIntervalMinutes <= 0)
                    errors.Add("Payments:TelegramTon:RateRefreshIntervalMinutes must be greater than 0.");

                if (telegramTon.MaxRateAgeMinutes <= 0)
                    errors.Add("Payments:TelegramTon:MaxRateAgeMinutes must be greater than 0.");

                if (telegramTon.MaxRateAgeMinutes < telegramTon.RateRefreshIntervalMinutes)
                    errors.Add("Payments:TelegramTon:MaxRateAgeMinutes must be greater than or equal to RateRefreshIntervalMinutes.");
            }

            if (!string.IsNullOrWhiteSpace(telegramTon.ApiBaseUrl)
                && !Uri.TryCreate(telegramTon.ApiBaseUrl, UriKind.Absolute, out _))
            {
                errors.Add("Payments:TelegramTon:ApiBaseUrl must be an absolute URI.");
            }

            if (telegramTon.RequestTimeoutSeconds <= 0)
                errors.Add("Payments:TelegramTon:RequestTimeoutSeconds must be greater than 0.");

            if (telegramTon.TransactionSearchLimit <= 0)
                errors.Add("Payments:TelegramTon:TransactionSearchLimit must be greater than 0.");

            if (telegramTon.PaymentTimeoutMinutes <= 0)
                errors.Add("Payments:TelegramTon:PaymentTimeoutMinutes must be greater than 0.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

