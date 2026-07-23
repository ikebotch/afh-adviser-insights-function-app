namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;

public sealed class DimCustomerEntity
{
    public string? EntitySk { get; set; }

    public string? ClientEntityId { get; set; }

    public string? EntityName { get; set; }

    public string? Household { get; set; }
}
