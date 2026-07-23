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

    public string? InternalToken { get; set; }

    public string CurrentUserPath { get; set; } = "api/internal/identity/v1/me";
}

public sealed class SnowflakeOptions
{
    public string? ConnectionString { get; set; }
}
