using System.Globalization;
using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Contract;
using AFH.AdviserInsights.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Infrastructure.Clients;

public sealed class SnowflakeAdviserInsightsRepository(
    ISnowflakeSqlClient snowflake,
    IOptions<AdviserInsightsOptions> options) : IAdviserInsightsRepository
{
    public async Task<AdviserProfileResponse?> GetAdviserProfileAsync(AdviserDataScope scope, CancellationToken cancellationToken)
    {
        var rows = await snowflake.QueryAsync($"""
            SELECT
                ADVISER_ID,
                ADVISER,
                EMAIL_ADDRESS,
                ADVISER_MANAGER,
                REGION,
                ADVISER_STATUS
            FROM {Table("DIM_ADVISER")}
            WHERE {AdviserIdentityFilter(scope, "ADVISER_ID", "EMAIL_ADDRESS", "ADVISER")}
            LIMIT 1
            """, cancellationToken).ConfigureAwait(false);

        var row = rows.FirstOrDefault();
        return row is null
            ? null
            : new AdviserProfileResponse(
                Text(row, "ADVISER_ID") ?? string.Empty,
                Text(row, "ADVISER") ?? string.Empty,
                Text(row, "EMAIL_ADDRESS"),
                Text(row, "ADVISER_MANAGER"),
                Text(row, "REGION"),
                Text(row, "ADVISER_STATUS"),
                scope.AccessMode);
    }

    public async Task<IReadOnlyList<TeamAdviserResponse>> GetTeamAdvisersAsync(AdviserDataScope scope, CancellationToken cancellationToken)
    {
        var rows = await snowflake.QueryAsync($"""
            SELECT
                ADVISER_ID,
                ADVISER,
                EMAIL_ADDRESS,
                ADVISER_MANAGER,
                REGION,
                ADVISER_STATUS
            FROM {Table("DIM_ADVISER")}
            WHERE {TeamAdviserFilter(scope, "ADVISER_MANAGER")}
            ORDER BY ADVISER
            LIMIT 100
            """, cancellationToken).ConfigureAwait(false);

        return rows.Select(row => new TeamAdviserResponse(
            Text(row, "ADVISER_ID") ?? string.Empty,
            Text(row, "ADVISER") ?? string.Empty,
            Text(row, "EMAIL_ADDRESS"),
            Text(row, "ADVISER_MANAGER"),
            Text(row, "REGION"),
            Text(row, "ADVISER_STATUS"))).ToArray();
    }

    public async Task<IReadOnlyList<ClientSummaryResponse>> GetClientsAsync(
        AdviserDataScope scope,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = await snowflake.QueryAsync($"""
            SELECT
                c.CLIENT_ID,
                c.ENTITYNAME AS CLIENT_NAME,
                a.ADVISER_ID,
                a.ADVISER AS ADVISER_NAME,
                c.HOUSEHOLD,
                SUM(COALESCE(f.AUM_VALUE, 0)) AS AUM_VALUE
            FROM {Table("DIM_CUSTOMER")} c
            INNER JOIN {Table("DIM_ADVISER")} a ON a.ADVISER_ID = c.ADVISER_ID
            LEFT JOIN {Table("FACT_AUM")} f ON f.CLIENT_ID = c.CLIENT_ID
            WHERE {ScopeFilter(scope, "a")}
            GROUP BY c.CLIENT_ID, c.ENTITYNAME, a.ADVISER_ID, a.ADVISER, c.HOUSEHOLD
            ORDER BY AUM_VALUE DESC, CLIENT_NAME
            LIMIT {pageSize}
            """, cancellationToken).ConfigureAwait(false);

        return rows.Select(row => new ClientSummaryResponse(
            Text(row, "CLIENT_ID") ?? string.Empty,
            Text(row, "CLIENT_NAME"),
            Text(row, "ADVISER_ID"),
            Text(row, "ADVISER_NAME"),
            Text(row, "HOUSEHOLD"),
            Number(row, "AUM_VALUE"))).ToArray();
    }

    public async Task<IReadOnlyList<PolicySummaryResponse>> GetPoliciesAsync(
        AdviserDataScope scope,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = await snowflake.QueryAsync($"""
            SELECT
                p.XPLAN_POLICY_SK AS POLICY_ID,
                c.CLIENT_ID,
                c.ENTITYNAME AS CLIENT_NAME,
                a.ADVISER_ID,
                a.ADVISER AS ADVISER_NAME,
                ps.REPORT_NAME,
                ps.POLICY_START_DATE,
                ps.POLICY_SERVICE_CLOSE_DATE,
                SUM(COALESCE(f.AUM_VALUE, 0)) AS AUM_VALUE
            FROM {Table("DIM_CUSTOMER")} c
            INNER JOIN {Table("DIM_ADVISER")} a ON a.ADVISER_ID = c.ADVISER_ID
            INNER JOIN {Table("DIM_CUSTOMER_X_POLICY")} cp ON cp.CLIENT_ID = c.CLIENT_ID
            INNER JOIN {Table("DIM_XPLAN_POLICY")} p ON p.XPLAN_POLICY_SK = cp.XPLAN_POLICY_SK
            LEFT JOIN {Table("DIM_POLICY_SERVICE_START_END_DATE")} ps ON ps.XPLAN_POLICY_SK = p.XPLAN_POLICY_SK
            LEFT JOIN {Table("FACT_AUM")} f ON f.XPLAN_POLICY_SK = p.XPLAN_POLICY_SK
            WHERE {ScopeFilter(scope, "a")}
            GROUP BY p.XPLAN_POLICY_SK, c.CLIENT_ID, c.ENTITYNAME, a.ADVISER_ID, a.ADVISER, ps.REPORT_NAME, ps.POLICY_START_DATE, ps.POLICY_SERVICE_CLOSE_DATE
            ORDER BY AUM_VALUE DESC, CLIENT_NAME
            LIMIT {pageSize}
            """, cancellationToken).ConfigureAwait(false);

        return rows.Select(row => new PolicySummaryResponse(
            Text(row, "POLICY_ID") ?? string.Empty,
            Text(row, "CLIENT_ID"),
            Text(row, "CLIENT_NAME"),
            Text(row, "ADVISER_ID"),
            Text(row, "ADVISER_NAME"),
            Text(row, "REPORT_NAME"),
            Date(row, "POLICY_START_DATE"),
            Date(row, "POLICY_SERVICE_CLOSE_DATE"),
            Number(row, "AUM_VALUE"))).ToArray();
    }

    public async Task<AumSummaryResponse> GetAumSummaryAsync(AdviserDataScope scope, CancellationToken cancellationToken)
    {
        var rows = await snowflake.QueryAsync($"""
            SELECT
                COUNT(DISTINCT a.ADVISER_ID) AS ADVISER_COUNT,
                COUNT(DISTINCT c.CLIENT_ID) AS CLIENT_COUNT,
                COUNT(DISTINCT f.XPLAN_POLICY_SK) AS POLICY_COUNT,
                SUM(COALESCE(f.AUM_VALUE, 0)) AS TOTAL_AUM
            FROM {Table("DIM_CUSTOMER")} c
            INNER JOIN {Table("DIM_ADVISER")} a ON a.ADVISER_ID = c.ADVISER_ID
            LEFT JOIN {Table("FACT_AUM")} f ON f.CLIENT_ID = c.CLIENT_ID
            WHERE {ScopeFilter(scope, "a")}
            """, cancellationToken).ConfigureAwait(false);

        var row = rows.FirstOrDefault() ?? new Dictionary<string, object?>();
        return new AumSummaryResponse(
            scope.AccessMode,
            (int)(Number(row, "ADVISER_COUNT") ?? 0),
            (int)(Number(row, "CLIENT_COUNT") ?? 0),
            (int)(Number(row, "POLICY_COUNT") ?? 0),
            Number(row, "TOTAL_AUM") ?? 0);
    }

    public async Task<IReadOnlyList<HighValueClientResponse>> GetHighValueClientsAsync(
        AdviserDataScope scope,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = await snowflake.QueryAsync($"""
            SELECT
                c.CLIENT_ID,
                c.ENTITYNAME AS CLIENT_NAME,
                a.ADVISER_ID,
                a.ADVISER AS ADVISER_NAME,
                SUM(COALESCE(f.AUM_VALUE, 0)) AS TOTAL_POLICY_VALUE,
                COUNT(DISTINCT f.XPLAN_POLICY_SK) AS POLICY_COUNT
            FROM {Table("DIM_CUSTOMER")} c
            INNER JOIN {Table("DIM_ADVISER")} a ON a.ADVISER_ID = c.ADVISER_ID
            LEFT JOIN {Table("FACT_AUM")} f ON f.CLIENT_ID = c.CLIENT_ID
            WHERE {ScopeFilter(scope, "a")}
            GROUP BY c.CLIENT_ID, c.ENTITYNAME, a.ADVISER_ID, a.ADVISER
            ORDER BY TOTAL_POLICY_VALUE DESC
            LIMIT {pageSize}
            """, cancellationToken).ConfigureAwait(false);

        return rows.Select(row => new HighValueClientResponse(
            Text(row, "CLIENT_ID") ?? string.Empty,
            Text(row, "CLIENT_NAME"),
            Text(row, "ADVISER_ID"),
            Text(row, "ADVISER_NAME"),
            Number(row, "TOTAL_POLICY_VALUE") ?? 0,
            (int)(Number(row, "POLICY_COUNT") ?? 0))).ToArray();
    }

    public async Task<IReadOnlyList<MissingAnnualReviewClientResponse>> GetClientsMissingAnnualReviewAsync(
        AdviserDataScope scope,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = await snowflake.QueryAsync($"""
            SELECT
                c.CLIENT_ID,
                c.ENTITYNAME AS CLIENT_NAME,
                a.ADVISER_ID,
                a.ADVISER AS ADVISER_NAME,
                MAX(ps.POLICY_SERVICE_CLOSE_DATE) AS LAST_POLICY_SERVICE_DATE,
                COUNT(DISTINCT p.XPLAN_POLICY_SK) AS ACTIVE_POLICY_COUNT
            FROM {Table("DIM_CUSTOMER")} c
            INNER JOIN {Table("DIM_ADVISER")} a ON a.ADVISER_ID = c.ADVISER_ID
            INNER JOIN {Table("DIM_CUSTOMER_X_POLICY")} cp ON cp.CLIENT_ID = c.CLIENT_ID
            INNER JOIN {Table("DIM_XPLAN_POLICY")} p ON p.XPLAN_POLICY_SK = cp.XPLAN_POLICY_SK
            LEFT JOIN {Table("DIM_POLICY_SERVICE_START_END_DATE")} ps ON ps.XPLAN_POLICY_SK = p.XPLAN_POLICY_SK
            WHERE {ScopeFilter(scope, "a")}
            GROUP BY c.CLIENT_ID, c.ENTITYNAME, a.ADVISER_ID, a.ADVISER
            HAVING MAX(ps.POLICY_SERVICE_CLOSE_DATE) IS NULL
                OR MAX(ps.POLICY_SERVICE_CLOSE_DATE) < DATEADD(year, -1, CURRENT_DATE())
            ORDER BY LAST_POLICY_SERVICE_DATE NULLS FIRST, CLIENT_NAME
            LIMIT {pageSize}
            """, cancellationToken).ConfigureAwait(false);

        return rows.Select(row => new MissingAnnualReviewClientResponse(
            Text(row, "CLIENT_ID") ?? string.Empty,
            Text(row, "CLIENT_NAME"),
            Text(row, "ADVISER_ID"),
            Text(row, "ADVISER_NAME"),
            Date(row, "LAST_POLICY_SERVICE_DATE"),
            (int)(Number(row, "ACTIVE_POLICY_COUNT") ?? 0))).ToArray();
    }

    private string Table(string tableName)
    {
        var snowflake = options.Value.Snowflake;
        return $"{Identifier(snowflake.Database)}.{Identifier(snowflake.Schema)}.{Identifier(tableName)}";
    }

    private static string ScopeFilter(AdviserDataScope scope, string adviserAlias)
    {
        if (scope.IncludeAll)
            return "1 = 1";

        if (scope.IncludeTeam)
            return $"({adviserAlias}.ADVISER_MANAGER = {Sql(scope.ManagerName)} OR {adviserAlias}.ADVISER_ID = {Sql(scope.AdviserId)} OR LOWER({adviserAlias}.EMAIL_ADDRESS) = LOWER({Sql(scope.Email)}))";

        return AdviserIdentityFilter(scope, $"{adviserAlias}.ADVISER_ID", $"{adviserAlias}.EMAIL_ADDRESS", $"{adviserAlias}.ADVISER");
    }

    private static string TeamAdviserFilter(AdviserDataScope scope, string managerColumn)
        => scope.IncludeAll
            ? "1 = 1"
            : $"{managerColumn} = {Sql(scope.ManagerName)}";

    private static string AdviserIdentityFilter(AdviserDataScope scope, string adviserIdColumn, string emailColumn, string adviserNameColumn)
        => $"({adviserIdColumn} = {Sql(scope.AdviserId)} OR LOWER({emailColumn}) = LOWER({Sql(scope.Email)}) OR {adviserNameColumn} = {Sql(scope.ManagerName)})";

    private static string Identifier(string value) => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string Sql(string? value) => value is null ? "NULL" : "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static string? Text(IReadOnlyDictionary<string, object?> row, string name)
        => row.TryGetValue(name, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static decimal? Number(IReadOnlyDictionary<string, object?> row, string name)
        => row.TryGetValue(name, out var value) && decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static DateOnly? Date(IReadOnlyDictionary<string, object?> row, string name)
        => row.TryGetValue(name, out var value) && DateOnly.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
}
