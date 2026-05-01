using Microsoft.Extensions.Options;
using TonSdk.Client;
using TonSdk.Contracts.Wallet;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace MiniApp.Features.Payments;

public sealed class TelegramTonHotWalletService : ITelegramTonHotWalletService
{
    private readonly PaymentsOptions _options;
    private readonly ILogger<TelegramTonHotWalletService> _logger;

    public TelegramTonHotWalletService(IOptions<PaymentsOptions> options, ILogger<TelegramTonHotWalletService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TelegramTonHotWalletStateResult> GetHotWalletStateAsync(CancellationToken ct)
    {
        try
        {
            var context = CreateContext();
            using var client = CreateTonClient();

            var isDeployed = await client.IsContractDeployed(context.Wallet.Address);
            var infoResult = await client.GetWalletInformation(context.Wallet.Address);
            if (!infoResult.HasValue)
                return new TelegramTonHotWalletStateResult(false, "TON wallet information is unavailable.");

            var info = infoResult.Value;

            var balanceTon = info.Balance.ToDecimal();
            var seqno = info.Seqno is { } value ? checked((int)value) : 0;

            return new TelegramTonHotWalletStateResult(
                true,
                Address: context.Wallet.Address.ToString(),
                BalanceTon: balanceTon,
                Seqno: seqno,
                IsDeployed: isDeployed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Telegram TON hot wallet state.");
            return new TelegramTonHotWalletStateResult(false, ex.Message);
        }
    }

    public async Task<TelegramTonSendWithdrawalResult> SendWithdrawalAsync(TelegramTonSendWithdrawalRequest request, CancellationToken ct)
    {
        try
        {
            var context = CreateContext();
            var destination = new Address(request.DestinationAddress.Trim());

            using var client = CreateTonClient();
            var isDeployed = await client.IsContractDeployed(context.Wallet.Address);
            if (!isDeployed)
                return new TelegramTonSendWithdrawalResult(false, "TON hot wallet is not deployed on-chain.");

            var walletInfoResult = await client.GetWalletInformation(context.Wallet.Address);
            if (!walletInfoResult.HasValue)
                return new TelegramTonSendWithdrawalResult(false, "TON wallet information is unavailable.");

            var walletInfo = walletInfoResult.Value;

            var currentSeqno = walletInfo.Seqno is { } seqnoValue
                ? checked((int)seqnoValue)
                : 0;
            if (currentSeqno != request.Seqno)
            {
                return new TelegramTonSendWithdrawalResult(
                    false,
                    $"TON hot wallet seqno changed from reserved value {request.Seqno} to {currentSeqno}. Retry with a fresh reservation.");
            }

            var seqno = currentSeqno;
            var balanceTon = walletInfo.Balance.ToDecimal();
            var minReserveTon = Math.Max(_options.TelegramTon.HotWalletMinReserveTon, 0m);
            var availableBalanceText = balanceTon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var requiredBalanceText = (request.AmountTon + minReserveTon).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (balanceTon < request.AmountTon + minReserveTon)
            {
                return new TelegramTonSendWithdrawalResult(
                    false,
                    "TON hot wallet balance is insufficient. Available " + availableBalanceText + " TON, required " + requiredBalanceText + " TON including reserve.");
            }

            var body = new CellBuilder(1023)
                .StoreUInt(0, 32, true)
                .StoreString(request.Memo, true)
                .Build();

            var message = new InternalMessage(new InternalMessageOptions
            {
                Info = new IntMsgInfo(new IntMsgInfoOptions
                {
                    Bounce = request.Bounce,
                    Dest = destination,
                    Value = new Coins(ToNanoString(request.AmountTon), new CoinsOptions(true, 9))
                }),
                Body = body
            });

            var validUntil = checked((uint)DateTimeOffset.UtcNow.AddSeconds(Math.Max(request.ValidForSeconds, 30)).ToUnixTimeSeconds());
            var external = context.Wallet.CreateTransferMessage(
                [new WalletTransfer { Message = message, Mode = 3 }],
                checked((uint)seqno),
                validUntil);
            var signed = external.Sign(context.Keys.PrivateKey, false);

            var resultValue = await client.SendBoc(signed.Cell);
            if (!resultValue.HasValue || string.IsNullOrWhiteSpace(resultValue.Value.Hash))
                return new TelegramTonSendWithdrawalResult(false, "TON wallet broadcast did not return a message hash.");

            var result = resultValue.Value;

            return new TelegramTonSendWithdrawalResult(
                true,
                ExternalMessageHash: result.Hash,
                Seqno: seqno,
                SubmittedAtUtc: DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send a server-executed Telegram TON withdrawal.");
            return new TelegramTonSendWithdrawalResult(false, ex.Message);
        }
    }

    public async Task<TelegramTonOutgoingTransferLookupResult> TryFindOutgoingTransferAsync(TelegramTonOutgoingTransferLookupRequest request, CancellationToken ct)
    {
        try
        {
            var context = CreateContext();
            var normalizedDestination = NormalizeAddress(request.DestinationAddress);

            using var client = CreateTonClient();
            var transactions = await client.GetTransactions(
                context.Wallet.Address,
                checked((uint)Math.Clamp(request.SearchLimit, 1, 100)),
                null,
                null,
                null,
                true);

            foreach (var tx in transactions ?? [])
            {
                var observedAtUtc = DateTimeOffset.FromUnixTimeSeconds(tx.UTime);
                if (observedAtUtc < request.CreatedAfterUtc.AddMinutes(-2))
                    continue;

                foreach (var outMsg in tx.OutMsgs ?? [])
                {
                    if (outMsg.Destination is null)
                        continue;

                    if (!string.Equals(NormalizeAddress(outMsg.Destination.ToString()), normalizedDestination, StringComparison.Ordinal))
                        continue;

                    if (!string.Equals((outMsg.Message ?? string.Empty).Trim(), request.Memo.Trim(), StringComparison.Ordinal))
                        continue;

                    var amountTon = outMsg.Value.ToDecimal();
                    if (amountTon + 0.000000001m < request.AmountTon)
                        continue;

                    return new TelegramTonOutgoingTransferLookupResult(
                        true,
                        true,
                        TransactionHash: tx.TransactionId.Hash,
                        ObservedAtUtc: observedAtUtc,
                        AmountTon: amountTon);
                }
            }

            return new TelegramTonOutgoingTransferLookupResult(true, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconcile outgoing Telegram TON withdrawal transfer.");
            return new TelegramTonOutgoingTransferLookupResult(false, false, ex.Message);
        }
    }

    private TonHotWalletContext CreateContext()
    {
        var telegramTon = _options.TelegramTon;
        if (!telegramTon.ServerWithdrawalsEnabled)
            throw new InvalidOperationException("Server-executed TON withdrawals are disabled.");

        var words = telegramTon.HotWalletMnemonic
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length < 12)
            throw new InvalidOperationException("TON hot wallet mnemonic is invalid or incomplete.");

        if (!Utils.IsMnemonicValid(words))
            throw new InvalidOperationException("TON hot wallet mnemonic failed validation.");

        var mnemonic = new Mnemonic(words);
        var revision = checked((uint)Math.Max(telegramTon.HotWalletRevision, 1));
        var subwalletId = checked((uint)Math.Max(telegramTon.HotWalletSubwalletId, 0));
        var workchain = telegramTon.HotWalletWorkchain;

        var wallet = new WalletV4(new WalletV4Options
        {
            PublicKey = mnemonic.Keys.PublicKey,
            Workchain = workchain,
            SubwalletId = subwalletId
        }, revision);

        var expectedAddress = telegramTon.HotWalletExpectedAddress.Trim();
        if (!string.IsNullOrWhiteSpace(expectedAddress))
        {
            var derivedAddress = NormalizeAddress(wallet.Address.ToString());
            var normalizedExpectedAddress = NormalizeAddress(expectedAddress);
            if (!string.Equals(derivedAddress, normalizedExpectedAddress, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Derived TON hot wallet address does not match Payments:TelegramTon:HotWalletExpectedAddress."
                    + " Derived=" + derivedAddress
                    + "; Expected=" + normalizedExpectedAddress + "."
                    + " Check mnemonic, hot wallet workchain, revision, and subwallet id.");
            }
        }

        return new TonHotWalletContext(wallet, mnemonic.Keys);
    }

    private TonClient CreateTonClient()
    {
        var telegramTon = _options.TelegramTon;
        var endpoint = telegramTon.ApiBaseUrl.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
            endpoint = "https://toncenter.com/api/v2/";

        return new TonClient(
            TonClientType.HTTP_TONCENTERAPIV2,
            new HttpParameters
            {
                Endpoint = endpoint,
                ApiKey = string.IsNullOrWhiteSpace(telegramTon.ApiKey) ? null : telegramTon.ApiKey.Trim(),
                Timeout = GetTonSdkTimeoutMilliseconds(telegramTon.RequestTimeoutSeconds)
            });
    }

    private static int GetTonSdkTimeoutMilliseconds(int requestTimeoutSeconds)
    {
        var timeoutSeconds = requestTimeoutSeconds <= 0 ? 15 : requestTimeoutSeconds;
        return checked(timeoutSeconds * 1000);
    }

    private static string NormalizeAddress(string address)
        => new Address(address.Trim()).ToString(
            AddressType.Raw,
            new AddressStringifyOptions(true, true, false, 0)
            {
                Workchain = null
            });

    private static string ToNanoString(decimal amountTon)
    {
        var nanos = decimal.Round(amountTon * 1_000_000_000m, 0, MidpointRounding.AwayFromZero);
        return nanos.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed record TonHotWalletContext(WalletV4 Wallet, KeyPair Keys);
}






