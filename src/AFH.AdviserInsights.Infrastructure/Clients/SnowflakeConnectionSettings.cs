using AFH.AdviserInsights.Infrastructure.Options;

namespace AFH.AdviserInsights.Infrastructure.Clients;

internal sealed record SnowflakeConnectionSettings(
    Uri AccountUrl,
    string? ApiToken,
    string Authenticator,
    string? Account,
    string? User,
    string? PrivateKey,
    string? PrivateKeyFile,
    string Warehouse,
    string Database,
    string Schema,
    string Role,
    TimeSpan Timeout)
{
    public static SnowflakeConnectionSettings FromOptions(SnowflakeOptions options)
    {
        var values = ParseConnectionString(options.ConnectionString);

        var accountUrl = ResolveAccountUrl(
            GetValue(values, "accounturl") ?? options.AccountUrl,
            GetValue(values, "host") ?? options.Host);

        return new SnowflakeConnectionSettings(
            accountUrl,
            GetValue(values, "apitoken") ?? options.ApiToken,
            GetValue(values, "authenticator") ?? options.Authenticator ?? "oauth",
            GetValue(values, "account") ?? options.Account,
            GetValue(values, "user") ?? options.User,
            GetValue(values, "private_key") ?? GetValue(values, "privatekey") ?? options.PrivateKey,
            GetValue(values, "private_key_file") ?? GetValue(values, "privatekeyfile") ?? options.PrivateKeyFile,
            GetValue(values, "warehouse") ?? options.Warehouse,
            GetValue(values, "database") ?? options.Database,
            GetValue(values, "schema") ?? options.Schema,
            GetValue(values, "role") ?? options.Role,
            options.Timeout);
    }

    private static Uri ResolveAccountUrl(string? accountUrl, string? host)
    {
        if (!string.IsNullOrWhiteSpace(accountUrl))
            return new Uri(accountUrl.TrimEnd('/') + "/");

        if (!string.IsNullOrWhiteSpace(host))
        {
            var trimmedHost = host.Trim().TrimEnd('/');
            var url = trimmedHost.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || trimmedHost.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? trimmedHost
                    : $"https://{trimmedHost}";
            return new Uri(url.TrimEnd('/') + "/");
        }

        throw new InvalidOperationException("AdviserInsights:Snowflake:AccountUrl or AdviserInsights:Snowflake:Host is required.");
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static IReadOnlyDictionary<string, string> ParseConnectionString(string? connectionString)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(connectionString))
            return values;

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = part[..separator].Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
            var value = part[(separator + 1)..].Trim().Trim('"');
            values[key] = value;
        }

        return values;
    }
}
