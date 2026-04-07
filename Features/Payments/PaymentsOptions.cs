using Microsoft.Extensions.Options;

namespace MiniApp.Features.Payments;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    public bool Enabled { get; set; }

    public BtcPayOptions BtcPay { get; set; } = new();

    public PaymentsOpsOptions Ops { get; set; } = new();
}

public sealed class BtcPayOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string StoreId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public string DefaultCurrency { get; set; } = "USD";

    public int RequestTimeoutSeconds { get; set; } = 15;

    public int MaxRetryAttempts { get; set; } = 3;
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

        if (string.IsNullOrWhiteSpace(btcPay.BaseUrl))
            errors.Add("Payments:BtcPay:BaseUrl is required when payments are enabled.");
        else if (!Uri.TryCreate(btcPay.BaseUrl, UriKind.Absolute, out _))
            errors.Add("Payments:BtcPay:BaseUrl must be an absolute URI.");

        if (string.IsNullOrWhiteSpace(btcPay.StoreId))
            errors.Add("Payments:BtcPay:StoreId is required when payments are enabled.");

        if (string.IsNullOrWhiteSpace(btcPay.ApiKey))
            errors.Add("Payments:BtcPay:ApiKey is required when payments are enabled.");

        if (btcPay.RequestTimeoutSeconds <= 0)
            errors.Add("Payments:BtcPay:RequestTimeoutSeconds must be greater than 0.");

        if (btcPay.MaxRetryAttempts <= 0)
            errors.Add("Payments:BtcPay:MaxRetryAttempts must be greater than 0.");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

