using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniApp.TelegramLogin;

/// <summary>
/// Validates Telegram Mini App initData according to Telegram docs.
/// </summary>
public static class TelegramInitDataValidator
{
    public static bool TryValidateInitData(string initData, string botToken, TimeSpan maxAge, out TelegramInitDataUser? user, out string? error)
        => TryValidateInitData(initData, botToken, maxAge, out user, out _, out error);

    public static bool TryValidateInitData(
        string initData,
        string botToken,
        TimeSpan maxAge,
        out TelegramInitDataUser? user,
        out string? startParam,
        out string? error)
    {
        user = null;
        startParam = null;
        error = null;

        if (string.IsNullOrWhiteSpace(initData))
        {
            error = "initData is empty.";
            return false;
        }

        var dict = ParseQueryString(initData);
        startParam = NormalizeStartParam(dict.TryGetValue("start_param", out var parsedStartParam) ? parsedStartParam : null);

        if (!dict.TryGetValue("hash", out var hash) || string.IsNullOrWhiteSpace(hash))
        {
            error = "initData is missing hash.";
            return false;
        }

        // auth_date is required for age validation
        if (!dict.TryGetValue("auth_date", out var authDateStr) || !long.TryParse(authDateStr, out var authDateSeconds))
        {
            error = "initData is missing/invalid auth_date.";
            return false;
        }

        var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (maxAge > TimeSpan.Zero)
        {
            var age = TimeSpan.FromSeconds(Math.Abs(nowSeconds - authDateSeconds));
            if (age > maxAge)
            {
                error = $"initData is too old ({age}).";
                return false;
            }
        }

        // compute data-check-string
        var pairs = dict
            .Where(kv => kv.Key != "hash")
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}");

        var dataCheckString = string.Join("\n", pairs);

        // Telegram WebApp initData secret key:
        // secret_key = HMAC_SHA256(key="WebAppData", msg=bot_token)
        var secretKey = ComputeHmacSha256(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var computedHash = ComputeHmacSha256Hex(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        if (!CryptographicEquals(hash, computedHash))
        {
            error = "initData hash mismatch.";
            return false;
        }

        if (!dict.TryGetValue("user", out var userJson) || string.IsNullOrWhiteSpace(userJson))
        {
            error = "initData is missing user.";
            return false;
        }

        try
        {
            user = JsonSerializer.Deserialize<TelegramInitDataUser>(userJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (user is null || user.Id <= 0)
            {
                error = "initData user is invalid.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = "Failed to parse initData user: " + ex.Message;
            return false;
        }
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        // initData uses query-string format without leading '?'
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;

            var key = Uri.UnescapeDataString(part[..idx]);
            var value = Uri.UnescapeDataString(part[(idx + 1)..]);
            dict[key] = value;
        }

        return dict;
    }

    private static byte[] ComputeSha256(byte[] data)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(data);
    }

    private static byte[] ComputeHmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static string ComputeHmacSha256Hex(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool CryptographicEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static string? NormalizeStartParam(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > 128)
            trimmed = trimmed[..128];

        return trimmed;
    }
}

public sealed class TelegramInitDataUser
{
    public long Id { get; set; }
    public string? Username { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; set; }
}
