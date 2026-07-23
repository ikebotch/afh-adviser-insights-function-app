namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;

public sealed class DimAdviserEntity
{
    public string? AdviserSk { get; set; }

    public string? AdviserId { get; set; }

    public string? Adviser { get; set; }

    public string? EmailAddress { get; set; }

    public string? AdviserManager { get; set; }

    public string? Region { get; set; }

    public string? AdviserStatus { get; set; }
}
