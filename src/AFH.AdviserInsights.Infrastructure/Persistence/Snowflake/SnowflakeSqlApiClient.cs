using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using AFH.AdviserInsights.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake;

public sealed class SnowflakeSqlApiClient(
    HttpClient http,
    IOptions<AdviserInsightsOptions> options,
    TimeProvider timeProvider) : ISnowflakeSqlClient
{
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        string statement,
        CancellationToken cancellationToken)
    {
        var snowflake = SnowflakeConnectionSettings.FromOptions(options.Value.Snowflake);

        var endpoint = new Uri(snowflake.AccountUrl, "api/v2/statements");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.ParseAdd(MediaTypeNames.Application.Json);
        request.Headers.UserAgent.ParseAdd("AFH.AdviserInsights/1.0");
        AddAuthorizationHeader(request, snowflake);
        request.Content = JsonContent.Create(new SnowflakeStatementRequest(
            statement,
            snowflake.Timeout.TotalSeconds,
            snowflake.Database,
            snowflake.Schema,
            snowflake.Warehouse,
            snowflake.Role));

        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Snowflake SQL API returned HTTP {(int)response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        return MapRows(document.RootElement);
    }

    private void AddAuthorizationHeader(HttpRequestMessage request, SnowflakeConnectionSettings settings)
    {
        if (settings.Authenticator.Equals("snowflake_jwt", StringComparison.OrdinalIgnoreCase))
        {
            var jwt = SnowflakeJwtTokenFactory.CreateToken(settings, timeProvider);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");
            request.Headers.TryAddWithoutValidation("X-Snowflake-Authorization-Token-Type", "KEYPAIR_JWT");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.ApiToken))
            throw new InvalidOperationException("AdviserInsights:Snowflake:ApiToken is required unless Authenticator is snowflake_jwt.");

        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {settings.ApiToken}");
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> MapRows(JsonElement root)
    {
        var columns = root
            .GetProperty("resultSetMetaData")
            .GetProperty("rowType")
            .EnumerateArray()
            .Select(column => column.GetProperty("name").GetString() ?? string.Empty)
            .ToArray();

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return rows;

        foreach (var row in data.EnumerateArray())
        {
            var values = row.EnumerateArray().ToArray();
            var mapped = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < columns.Length && index < values.Length; index++)
            {
                mapped[columns[index]] = ToValue(values[index]);
            }

            rows.Add(mapped);
        }

        return rows;
    }

    private static object? ToValue(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => value.GetString()
        };

    private sealed record SnowflakeStatementRequest(
        string Statement,
        double Timeout,
        string Database,
        string Schema,
        string Warehouse,
        string Role);
}
