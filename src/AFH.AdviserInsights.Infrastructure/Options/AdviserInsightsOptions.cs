namespace AFH.AdviserInsights.Infrastructure.Options;

public sealed class AdviserInsightsOptions
{
    public const string SectionName = "AdviserInsights";

    public DownstreamIdentityOptions Identity { get; set; } = new();

    public SnowflakeOptions Snowflake { get; set; } = new();
}

public sealed class DownstreamIdentityOptions
{
    public string? BaseUrl { get; set; }

    public string? FunctionKey { get; set; }
}

public sealed class SnowflakeOptions
{
    public string? AccountUrl { get; set; }

    public string? ApiToken { get; set; }

    public string Warehouse { get; set; } = "DEFAULT";

    public string Database { get; set; } = "DIM_DB_DEV";

    public string Schema { get; set; } = "AFH";

    public string Role { get; set; } = "PUBLIC";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
