using AFH.AdviserInsights.Domain.Access;
using AFH.AdviserInsights.Infrastructure.Options;
using AFH.AdviserInsights.Infrastructure.Persistence.Snowflake;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Tests;

public sealed class SnowflakeAdviserInsightsRepositoryTests
{
    [Fact]
    public async Task GetClientsAsync_ForSelfScope_FiltersByAdviserIdentity()
    {
        var snowflake = new CapturingSnowflakeClient([
            new Dictionary<string, object?>
            {
                ["CLIENT_ID"] = "client-1",
                ["CLIENT_NAME"] = "Client One",
                ["ADVISER_ID"] = "adv-1",
                ["ADVISER_NAME"] = "Adviser One",
                ["HOUSEHOLD"] = "100",
                ["AUM_VALUE"] = "12345.67"
            }
        ]);
        var repository = CreateRepository(snowflake);
        var scope = new AdviserDataScope("Self", "adviser@afh.com", "adv-1", "Adviser One", false, false);

        var result = await repository.GetClientsAsync(scope, 10, CancellationToken.None);

        Assert.Single(result);
        Assert.Contains("FROM \"DIM_DB_DEV\".\"AFH\".\"DIM_CUSTOMER\"", snowflake.Statement, StringComparison.Ordinal);
        Assert.Contains("c.CLIENT_ENTITY_ID AS CLIENT_ID", snowflake.Statement, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN \"DIM_DB_DEV\".\"AFH\".\"FACT_CUSTOMER\" fc ON fc.ENTITY_SK = c.ENTITY_SK", snowflake.Statement, StringComparison.Ordinal);
        Assert.Contains("LEFT JOIN \"DIM_DB_DEV\".\"AFH\".\"FACT_AUM\" f ON f.HOUSEHOLD_SK = hc.HOUSEHOLD_SK", snowflake.Statement, StringComparison.Ordinal);
        Assert.Contains("COALESCE(f.ADJUSTED_VALUATION, f.VALUATION, 0)", snowflake.Statement, StringComparison.Ordinal);
        Assert.Contains("a.ADVISER_ID = 'adv-1'", snowflake.Statement, StringComparison.Ordinal);
        Assert.Equal(12345.67m, result[0].AumValue);
    }

    [Fact]
    public async Task GetTeamAdvisersAsync_ForTeamScope_FiltersByManagerName()
    {
        var snowflake = new CapturingSnowflakeClient([
            new Dictionary<string, object?>
            {
                ["ADVISER_ID"] = "adv-2",
                ["ADVISER"] = "Adviser Two",
                ["EMAIL_ADDRESS"] = "adviser2@afh.com",
                ["ADVISER_MANAGER"] = "Manager One",
                ["REGION"] = "North",
                ["ADVISER_STATUS"] = "Active"
            }
        ]);
        var repository = CreateRepository(snowflake);
        var scope = new AdviserDataScope("Team", "manager@afh.com", "mgr-1", "Manager One", true, false);

        var result = await repository.GetTeamAdvisersAsync(scope, CancellationToken.None);

        Assert.Single(result);
        Assert.Contains("ADVISER_MANAGER = 'Manager One'", snowflake.Statement, StringComparison.Ordinal);
        Assert.Equal("Adviser Two", result[0].AdviserName);
    }

    private static SnowflakeAdviserInsightsRepository CreateRepository(CapturingSnowflakeClient snowflake)
        => new(snowflake, Options.Create(new AdviserInsightsOptions()));

    private sealed class CapturingSnowflakeClient(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows) : ISnowflakeSqlClient
    {
        public string Statement { get; private set; } = string.Empty;

        public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(string statement, CancellationToken cancellationToken)
        {
            Statement = statement;
            return Task.FromResult(rows);
        }
    }
}
