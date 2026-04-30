using MiniApp.Data;

namespace MiniApp.Features.Wallet;

public sealed record WalletTopUpRequest(string InitData);

public sealed record WalletWithdrawRequest(string InitData, decimal Amount, string AssetCode = WithdrawalAssetCodes.Bitcoin, string? Address = null, bool SaveAddress = false, string? Number = null);

public sealed record WalletSaveAddressRequest(string InitData, string AssetCode = WithdrawalAssetCodes.Bitcoin, string Address = "");

public sealed record WalletGetAddressRequest(string InitData);

public sealed record WalletHistoryRequest(string InitData, int Limit = 50);


