namespace MiniApp.Data;

public sealed class DbOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// PostgreSQL connection string.
    /// Example: Host=localhost;Port=5432;Database=miniapp;Username=postgres;Password=postgres
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}

