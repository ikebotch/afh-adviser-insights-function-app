using System.Net.Http.Json;
using System.Text.Json;
using AFH.AdviserInsights.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Infrastructure.Clients;

public sealed class SnowflakeSqlApiClient(
    HttpClient http,
    IOptions<AdviserInsightsOptions> options) : ISnowflakeSqlClient
{
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        string statement,
        CancellationToken cancellationToken)
    {
        var snowflake = options.Value.Snowflake;
        if (string.IsNullOrWhiteSpace(snowflake.AccountUrl))
            throw new InvalidOperationException("AdviserInsights:Snowflake:AccountUrl is required.");
        if (string.IsNullOrWhiteSpace(snowflake.ApiToken))
            throw new InvalidOperationException("AdviserInsights:Snowflake:ApiToken is required.");

        var endpoint = new Uri(new Uri(snowflake.AccountUrl.TrimEnd('/') + "/"), "api/v2/statements");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {snowflake.ApiToken}");
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
