namespace MiniApp.Features.Wallet;

public sealed record WalletTopUpRequest(string InitData);

public sealed record WalletWithdrawRequest(string InitData, decimal Amount, string Number);

public sealed record WalletSaveAddressRequest(string InitData, string Address);

public sealed record WalletGetAddressRequest(string InitData);

public sealed record WalletHistoryRequest(string InitData, int Limit = 50);


