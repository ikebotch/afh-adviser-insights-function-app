namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;

public sealed class DimPolicyServicePeriodEntity
{
    public string? XplanPolicySk { get; set; }

    public string? ReportName { get; set; }

    public DateTime? PolicyStartDate { get; set; }

    public DateTime? PolicyServiceCloseDate { get; set; }
}
