namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;

public sealed class FactAumEntity
{
    public string? XplanPolicySk { get; set; }

    public string? HouseholdSk { get; set; }

    public decimal? Valuation { get; set; }

    public decimal? AdjustedValuation { get; set; }
}
