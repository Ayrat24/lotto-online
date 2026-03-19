namespace MiniApp.Admin;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>
    /// Admin username for the built-in cookie login.
    /// Store in User Secrets / env vars in real deployments.
    /// </summary>
    public string Username { get; init; } = "admin";

    /// <summary>
    /// Admin password for the built-in cookie login.
    /// Store in User Secrets / env vars in real deployments.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
