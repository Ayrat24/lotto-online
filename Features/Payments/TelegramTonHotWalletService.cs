using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.Json;
using TonSdk.Contracts;
using TonSdk.Contracts.Wallet;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace MiniApp.Features.Payments;

public sealed class TelegramTonHotWalletService : ITelegramTonHotWalletService
{
    private readonly HttpClient _http;
    private readonly PaymentsOptions _options;
    private readonly ILogger<TelegramTonHotWalletService> _logger;

    public TelegramTonHotWalletService(HttpClient http, IOptions<PaymentsOptions> options, ILogger<TelegramTonHotWalletService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TelegramTonHotWalletStateResult> GetHotWalletStateAsync(CancellationToken ct)
    {
        TonHotWalletDiagnosticsContext? diagnostics = null;
        TransferProbeResult transferProbe = new(false, null);
        try
        {
            diagnostics = BuildDiagnosticsContext();
            var context = CreateWalletContext(diagnostics);
            transferProbe = TryBuildTransferProbe(context, 0);
            var info = await GetWalletInformationAsync(context.Address, ct);

            return new TelegramTonHotWalletStateResult(
                true,
                DerivedAddress: diagnostics.DerivedAddress,
                ExpectedAddress: diagnostics.ExpectedAddress,
                Address: context.Address.ToString(),
                BalanceTon: info.BalanceTon,
                Seqno: info.Seqno,
                IsDeployed: info.IsDeployed,
                Workchain: diagnostics.Workchain,
                Revision: diagnostics.Revision,
                SubwalletId: diagnostics.SubwalletId,
                WalletVersion: diagnostics.WalletVersion,
                NetworkGlobalId: diagnostics.NetworkGlobalId,
                CanSignTransferProbe: transferProbe.Success,
                TransferProbeError: transferProbe.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Telegram TON hot wallet state.");
            return new TelegramTonHotWalletStateResult(
                false,
                FormatExceptionMessage(ex),
                DerivedAddress: diagnostics?.DerivedAddress,
                ExpectedAddress: diagnostics?.ExpectedAddress,
                Workchain: diagnostics?.Workchain,
                Revision: diagnostics?.Revision,
                SubwalletId: diagnostics?.SubwalletId,
                WalletVersion: diagnostics?.WalletVersion,
                NetworkGlobalId: diagnostics?.NetworkGlobalId,
                CanSignTransferProbe: transferProbe.Success,
                TransferProbeError: transferProbe.Error);
        }
    }

    public async Task<TelegramTonSendWithdrawalResult> SendWithdrawalAsync(TelegramTonSendWithdrawalRequest request, CancellationToken ct)
    {
        try
        {
            var context = CreateContext();
            var destination = new Address(request.DestinationAddress.Trim());
            var walletInfo = await GetWalletInformationAsync(context.Address, ct);
            if (!walletInfo.IsDeployed)
                return new TelegramTonSendWithdrawalResult(false, "TON hot wallet is not deployed on-chain.");

            if (walletInfo.Seqno is null)
                return new TelegramTonSendWithdrawalResult(false, "TON wallet information is unavailable.");

            var currentSeqno = walletInfo.Seqno.Value;
            if (currentSeqno != request.Seqno)
            {
                return new TelegramTonSendWithdrawalResult(
                    false,
                    $"TON hot wallet seqno changed from reserved value {request.Seqno} to {currentSeqno}. Retry with a fresh reservation.");
            }

            var seqno = currentSeqno;
            var balanceTon = walletInfo.BalanceTon;
            var minReserveTon = Math.Max(_options.TelegramTon.HotWalletMinReserveTon, 0m);
            var availableBalanceText = balanceTon.ToString(CultureInfo.InvariantCulture);
            var requiredBalanceText = (request.AmountTon + minReserveTon).ToString(CultureInfo.InvariantCulture);
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

            var signed = context.CreateSignedTransferMessage(
                [new WalletTransfer { Message = message, Mode = 3 }],
                checked((uint)seqno),
                request.ValidForSeconds);

            var hash = await SendBocAsync(signed.Cell, ct);
            if (string.IsNullOrWhiteSpace(hash))
                return new TelegramTonSendWithdrawalResult(false, "TON wallet broadcast did not return a message hash.");

            return new TelegramTonSendWithdrawalResult(
                true,
                ExternalMessageHash: hash,
                Seqno: seqno,
                SubmittedAtUtc: DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send a server-executed Telegram TON withdrawal.");
            return new TelegramTonSendWithdrawalResult(false, FormatExceptionMessage(ex));
        }
    }

    public async Task<TelegramTonOutgoingTransferLookupResult> TryFindOutgoingTransferAsync(TelegramTonOutgoingTransferLookupRequest request, CancellationToken ct)
    {
        try
        {
            var context = CreateContext();
            var normalizedDestination = NormalizeAddress(request.DestinationAddress);
            var transactions = await GetTransactionsAsync(context.Address.ToString(), Math.Clamp(request.SearchLimit, 1, 100), ct);

            foreach (var tx in transactions)
            {
                if (tx.ObservedAtUtc is null)
                    continue;

                if (tx.ObservedAtUtc.Value < request.CreatedAfterUtc.AddMinutes(-2))
                    continue;

                foreach (var outMsg in tx.OutMessages)
                {
                    if (string.IsNullOrWhiteSpace(outMsg.DestinationAddress))
                        continue;

                    if (!string.Equals(NormalizeAddress(outMsg.DestinationAddress), normalizedDestination, StringComparison.Ordinal))
                        continue;

                    if (!string.Equals((outMsg.Message ?? string.Empty).Trim(), request.Memo.Trim(), StringComparison.Ordinal))
                        continue;

                    if (outMsg.ValueTon + 0.000000001m < request.AmountTon)
                        continue;

                    return new TelegramTonOutgoingTransferLookupResult(
                        true,
                        true,
                        TransactionHash: tx.TransactionHash,
                        ObservedAtUtc: tx.ObservedAtUtc.Value,
                        AmountTon: outMsg.ValueTon);
                }
            }

            return new TelegramTonOutgoingTransferLookupResult(true, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconcile outgoing Telegram TON withdrawal transfer.");
            return new TelegramTonOutgoingTransferLookupResult(false, false, FormatExceptionMessage(ex));
        }
    }

    private TonHotWalletContext CreateContext()
        => CreateWalletContext(BuildDiagnosticsContext());

    private TonHotWalletDiagnosticsContext BuildDiagnosticsContext()
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
        var walletVersion = TelegramTonHotWalletVersions.Normalize(telegramTon.HotWalletVersion);
        if (walletVersion.Length == 0)
            throw new InvalidOperationException("Payments:TelegramTon:HotWalletVersion must be v4 or w5r1.");

        var subwalletId = checked((uint)Math.Max(telegramTon.HotWalletSubwalletId, 0));
        var workchain = telegramTon.HotWalletWorkchain;
        return walletVersion switch
        {
            TelegramTonHotWalletVersions.W5R1 => BuildWalletV5DiagnosticsContext(telegramTon, mnemonic, workchain, subwalletId),
            _ => BuildWalletV4DiagnosticsContext(telegramTon, mnemonic, workchain, subwalletId)
        };
    }

    private static TonHotWalletContext CreateWalletContext(TonHotWalletDiagnosticsContext diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(diagnostics.ExpectedAddress)
            && !string.Equals(diagnostics.DerivedAddress, diagnostics.ExpectedAddress, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Derived TON hot wallet address does not match Payments:TelegramTon:HotWalletExpectedAddress."
                + " Derived=" + diagnostics.DerivedAddress
                + "; Expected=" + diagnostics.ExpectedAddress + "."
                + " Check mnemonic, hot wallet version, workchain, revision/subwallet id, and W5 network global id.");
        }

        return diagnostics.Context;
    }

    private string GetNormalizedApiBaseUrl()
    {
        var endpoint = (_options.TelegramTon.ApiBaseUrl ?? string.Empty).Trim();
        if (endpoint.Length == 0)
            endpoint = "https://toncenter.com/api/v2/";
        if (!endpoint.EndsWith("/", StringComparison.Ordinal))
            endpoint += "/";

        return endpoint;
    }

    private static string NormalizeAddress(string address)
        => new Address(address.Trim()).ToString(
            AddressType.Raw,
            new AddressStringifyOptions(true, true, false, 0)
            {
                Workchain = null
            });

    private static string? NormalizeOptionalAddress(string? address)
        => string.IsNullOrWhiteSpace(address) ? null : NormalizeAddress(address);

    private static string ToNanoString(decimal amountTon)
    {
        var nanos = decimal.Round(amountTon * 1_000_000_000m, 0, MidpointRounding.AwayFromZero);
        return nanos.ToString("0", CultureInfo.InvariantCulture);
    }

    private static TransferProbeResult TryBuildTransferProbe(TonHotWalletContext context, int seqno)
    {
        try
        {
            var body = new CellBuilder(128)
                .StoreUInt(0, 32, true)
                .StoreString("TON-PROBE", true)
                .Build();

            var message = new InternalMessage(new InternalMessageOptions
            {
                Info = new IntMsgInfo(new IntMsgInfoOptions
                {
                    Bounce = false,
                    Dest = context.Address,
                    Value = new Coins("1", new CoinsOptions(true, 9))
                }),
                Body = body
            });

            _ = context.CreateSignedTransferMessage(
                [new WalletTransfer { Message = message, Mode = 3 }],
                checked((uint)Math.Max(seqno, 0)),
                600);

            return new TransferProbeResult(true, null);
        }
        catch (Exception ex)
        {
            return new TransferProbeResult(false, FormatExceptionMessage(ex));
        }
    }

    private async Task<ToncenterWalletInformation> GetWalletInformationAsync(Address walletAddress, CancellationToken ct)
    {
        using var document = await SendToncenterGetAsync(
            "getWalletInformation?address=" + Uri.EscapeDataString(walletAddress.ToString()),
            ct);

        var root = document.RootElement;
        if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("TON wallet information response did not contain a result object.");

        var balanceNanotons = TryGetDecimal(result, "balance") ?? 0m;
        var accountState = TryGetString(result, "account_state");
        var isWallet = TryGetBoolean(result, "wallet");
        var isDeployed = IsTonAccountDeployed(accountState, isWallet);
        var seqno = TryGetInt32(result, "seqno");

        return new ToncenterWalletInformation(
            decimal.Round(balanceNanotons / 1_000_000_000m, 9, MidpointRounding.AwayFromZero),
            seqno,
            isDeployed,
            accountState,
            isWallet);
    }

    private async Task<string> SendBocAsync(Cell cell, CancellationToken ct)
    {
        var boc = Convert.ToBase64String(BagOfCells.SerializeBoc(cell, false, true).ToBytes(false));
        using var document = await SendToncenterPostAsync("sendBocReturnHash", new { boc }, ct);

        var root = document.RootElement;
        if (!root.TryGetProperty("result", out var result))
            return Convert.ToBase64String(cell.Hash.ToBytes(false));

        return result.ValueKind switch
        {
            JsonValueKind.String => result.GetString() ?? Convert.ToBase64String(cell.Hash.ToBytes(false)),
            JsonValueKind.Object => TryGetString(result, "hash")
                ?? TryGetString(result, "message_hash")
                ?? Convert.ToBase64String(cell.Hash.ToBytes(false)),
            _ => Convert.ToBase64String(cell.Hash.ToBytes(false))
        };
    }

    private async Task<IReadOnlyList<ToncenterTransaction>> GetTransactionsAsync(string walletAddress, int limit, CancellationToken ct)
    {
        using var document = await SendToncenterGetAsync(
            "getTransactions?address=" + Uri.EscapeDataString(walletAddress.Trim())
            + "&limit=" + Math.Clamp(limit, 1, 100).ToString(CultureInfo.InvariantCulture)
            + "&archival=true",
            ct);

        var root = document.RootElement;
        if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
            return Array.Empty<ToncenterTransaction>();

        var transactions = new List<ToncenterTransaction>();
        foreach (var tx in result.EnumerateArray())
        {
            if (tx.ValueKind != JsonValueKind.Object)
                continue;

            var outMessages = new List<ToncenterOutMessage>();
            if (tx.TryGetProperty("out_msgs", out var outMsgs) && outMsgs.ValueKind == JsonValueKind.Array)
            {
                foreach (var outMsg in outMsgs.EnumerateArray())
                {
                    if (outMsg.ValueKind != JsonValueKind.Object)
                        continue;

                    var valueNanotons = TryGetDecimal(outMsg, "value") ?? 0m;
                    outMessages.Add(new ToncenterOutMessage(
                        TryGetString(outMsg, "destination"),
                        ExtractTonMessageText(outMsg),
                        decimal.Round(valueNanotons / 1_000_000_000m, 9, MidpointRounding.AwayFromZero)));
                }
            }

            transactions.Add(new ToncenterTransaction(
                ExtractTransactionHash(tx),
                TryGetUnixTime(tx, "utime"),
                outMessages));
        }

        return transactions;
    }

    private async Task<JsonDocument> SendToncenterGetAsync(string relativePathAndQuery, CancellationToken ct)
    {
        using var request = CreateToncenterRequest(HttpMethod.Get, relativePathAndQuery, null);
        var response = await _http.SendAsync(request, ct);
        return await ParseToncenterResponseAsync(response, ct);
    }

    private async Task<JsonDocument> SendToncenterPostAsync(string relativePath, object body, CancellationToken ct)
    {
        using var request = CreateToncenterRequest(HttpMethod.Post, relativePath, body);
        var response = await _http.SendAsync(request, ct);
        return await ParseToncenterResponseAsync(response, ct);
    }

    private HttpRequestMessage CreateToncenterRequest(HttpMethod method, string relativePathAndQuery, object? body)
    {
        var request = new HttpRequestMessage(method, new Uri(new Uri(GetNormalizedApiBaseUrl(), UriKind.Absolute), relativePathAndQuery));
        var apiKey = (_options.TelegramTon.ApiKey ?? string.Empty).Trim();
        if (apiKey.Length > 0)
            request.Headers.Add("X-API-Key", apiKey);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static async Task<JsonDocument> ParseToncenterResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Toncenter request failed with HTTP {(int)response.StatusCode}: {ExtractToncenterError(body)}");
        }

        var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            throw new InvalidOperationException("Toncenter response is not a JSON object.");
        }

        if (root.TryGetProperty("ok", out var okProp)
            && okProp.ValueKind == JsonValueKind.False)
        {
            var error = TryGetString(root, "error")
                ?? TryGetString(root, "description")
                ?? ExtractToncenterError(body);
            document.Dispose();
            throw new InvalidOperationException("Toncenter API error: " + error);
        }

        return document;
    }

    private static string ExtractToncenterError(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "Empty response body.";

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            return TryGetString(root, "error")
                ?? TryGetString(root, "description")
                ?? body.Trim();
        }
        catch
        {
            return body.Trim();
        }
    }

    private static string? ExtractTonMessageText(JsonElement message)
    {
        var direct = TryGetString(message, "message") ?? TryGetString(message, "comment");
        if (!string.IsNullOrWhiteSpace(direct))
            return NormalizeText(direct);

        if (message.TryGetProperty("msg_data", out var msgData) && msgData.ValueKind == JsonValueKind.Object)
        {
            var encoded = TryGetString(msgData, "text");
            if (!string.IsNullOrWhiteSpace(encoded))
            {
                var decoded = TryDecodeBase64Text(encoded);
                return NormalizeText(decoded ?? encoded);
            }
        }

        return null;
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().TrimEnd('\0');
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? TryDecodeBase64Text(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(value);
            var decoded = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractTransactionHash(JsonElement tx)
    {
        if (tx.TryGetProperty("transaction_id", out var txId) && txId.ValueKind == JsonValueKind.Object)
            return TryGetString(txId, "hash");

        return TryGetString(tx, "hash") ?? TryGetString(tx, "id");
    }

    private static DateTimeOffset? TryGetUnixTime(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var numeric))
            return DateTimeOffset.FromUnixTimeSeconds(numeric);

        if (prop.ValueKind == JsonValueKind.String
            && long.TryParse(prop.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out numeric))
        {
            return DateTimeOffset.FromUnixTimeSeconds(numeric);
        }

        return null;
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();

        return null;
    }

    private static bool? TryGetBoolean(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;

        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(prop.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static int? TryGetInt32(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var numeric))
            return numeric;

        if (prop.ValueKind == JsonValueKind.String
            && int.TryParse(prop.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out numeric))
        {
            return numeric;
        }

        return null;
    }

    private static decimal? TryGetDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var numeric))
            return numeric;

        if (prop.ValueKind == JsonValueKind.String
            && decimal.TryParse(prop.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out numeric))
        {
            return numeric;
        }

        return null;
    }

    private static bool IsTonAccountDeployed(string? accountState, bool? wallet)
    {
        var normalized = (accountState ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized is "uninitialized" or "nonexist")
            return false;
        if (wallet == true)
            return true;
        return normalized is "active" or "frozen";
    }

    private static string FormatExceptionMessage(Exception ex)
    {
        var parts = new List<string>();

        var current = ex;
        while (current is not null && parts.Count < 4)
        {
            var typeName = current.GetType().Name;
            var message = string.IsNullOrWhiteSpace(current.Message)
                ? typeName
                : typeName + ": " + current.Message.Trim();

            if (!parts.Contains(message, StringComparer.Ordinal))
                parts.Add(message);

            current = current.InnerException;
        }

        var formatted = string.Join(" | ", parts);
        return formatted.Length == 0 ? "Unhandled TON wallet exception." : formatted;
    }

    private TonHotWalletDiagnosticsContext BuildWalletV4DiagnosticsContext(
        TelegramTonOptions telegramTon,
        Mnemonic mnemonic,
        int workchain,
        uint subwalletId)
    {
        var revision = checked((uint)Math.Max(telegramTon.HotWalletRevision, 1));
        var wallet = new WalletV4(new WalletV4Options
        {
            PublicKey = mnemonic.Keys.PublicKey,
            Workchain = workchain,
            SubwalletId = subwalletId
        }, revision);

        return new TonHotWalletDiagnosticsContext(
            new TonHotWalletContext(
                wallet.Address,
                (transfers, seqno, validForSeconds) =>
                {
                    var validUntil = checked((uint)DateTimeOffset.UtcNow.AddSeconds(Math.Max(validForSeconds, 30)).ToUnixTimeSeconds());
                    var external = wallet.CreateTransferMessage(transfers, seqno, validUntil);
                    return external.Sign(mnemonic.Keys.PrivateKey, false);
                }),
            NormalizeAddress(wallet.Address.ToString()),
            NormalizeOptionalAddress(telegramTon.HotWalletExpectedAddress),
            workchain,
            checked((int)revision),
            checked((int)subwalletId),
            TelegramTonHotWalletVersions.V4,
            null);
    }

    private TonHotWalletDiagnosticsContext BuildWalletV5DiagnosticsContext(
        TelegramTonOptions telegramTon,
        Mnemonic mnemonic,
        int workchain,
        uint subwalletId)
    {
        if (subwalletId > TelegramTonHotWalletVersions.W5R1MaxSubwalletId)
        {
            throw new InvalidOperationException(
                $"Payments:TelegramTon:HotWalletSubwalletId={subwalletId} is invalid for W5 wallets."
                + $" W5 subwallet ids must be between 0 and {TelegramTonHotWalletVersions.W5R1MaxSubwalletId}.");
        }

        var networkGlobalId = telegramTon.HotWalletNetworkGlobalId != 0
            ? telegramTon.HotWalletNetworkGlobalId
            : TelegramTonNetworkGlobalIds.GetDefault(telegramTon);

        var wallet = new WalletV5(new WalletV5Options
        {
            PublicKey = mnemonic.Keys.PublicKey,
            Workchain = workchain,
            WalletId = new WalletIdV5R1<IWalletIdV5R1Context>
            {
                NetworkGlobalId = networkGlobalId,
                Context = new WalletIdV5R1ClientContext
                {
                    Version = WalletV5Version.V5R1,
                    SubwalletId = subwalletId
                }
            },
            SignatureAllowed = true,
            Seqno = 0,
            Extensions = []
        });

        return new TonHotWalletDiagnosticsContext(
            new TonHotWalletContext(
                wallet.Address,
                (transfers, seqno, validForSeconds) => wallet.CreateTransferMessage(
                    transfers,
                    seqno,
                    mnemonic.Keys.PrivateKey,
                    Math.Max(validForSeconds, 30))),
            NormalizeAddress(wallet.Address.ToString()),
            NormalizeOptionalAddress(telegramTon.HotWalletExpectedAddress),
            workchain,
            null,
            checked((int)subwalletId),
            TelegramTonHotWalletVersions.W5R1,
            networkGlobalId);
    }

    private sealed record TonHotWalletContext(
        Address Address,
        Func<WalletTransfer[], uint, int, ExternalInMessage> CreateSignedTransferMessage);

    private sealed record TransferProbeResult(bool Success, string? Error);

    private sealed record ToncenterWalletInformation(
        decimal BalanceTon,
        int? Seqno,
        bool IsDeployed,
        string? AccountState,
        bool? IsWallet);

    private sealed record ToncenterTransaction(
        string? TransactionHash,
        DateTimeOffset? ObservedAtUtc,
        IReadOnlyList<ToncenterOutMessage> OutMessages);

    private sealed record ToncenterOutMessage(
        string? DestinationAddress,
        string? Message,
        decimal ValueTon);

    private sealed record TonHotWalletDiagnosticsContext(
        TonHotWalletContext Context,
        string DerivedAddress,
        string? ExpectedAddress,
        int Workchain,
        int? Revision,
        int SubwalletId,
        string WalletVersion,
        int? NetworkGlobalId);
}

