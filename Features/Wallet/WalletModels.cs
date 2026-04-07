namespace MiniApp.Features.Wallet;

public sealed record WalletTopUpRequest(string InitData);

public sealed record WalletWithdrawRequest(string InitData, decimal Amount, string Number);


