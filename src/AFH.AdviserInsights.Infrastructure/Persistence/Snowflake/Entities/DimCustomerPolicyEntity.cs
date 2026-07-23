namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;

public sealed class DimCustomerPolicyEntity
{
    public string? XplanPolicySk { get; set; }

    public string? EntitySk { get; set; }

    public string? ClientEntityId { get; set; }

    public string? ReportName { get; set; }
}
