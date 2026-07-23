namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;

public sealed class FactCustomerEntity
{
    public string? EntitySk { get; set; }

    public string? HouseholdSk { get; set; }

    public string? AdviserSk { get; set; }
}
