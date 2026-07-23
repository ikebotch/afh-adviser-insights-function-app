namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;

public sealed class DimHouseholdCustomerEntity
{
    public string? HouseholdSk { get; set; }

    public string? EntitySk { get; set; }

    public string? ClientEntityId { get; set; }
}
