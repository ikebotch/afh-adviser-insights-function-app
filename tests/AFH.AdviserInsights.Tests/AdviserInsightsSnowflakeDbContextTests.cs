using AFH.AdviserInsights.Infrastructure.Persistence.Snowflake;
using AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;
using Microsoft.EntityFrameworkCore;

namespace AFH.AdviserInsights.Tests;

public sealed class AdviserInsightsSnowflakeDbContextTests
{
    [Fact]
    public void Model_MapsDimensionalSnowflakeTablesAsReadOnlyKeylessEntities()
    {
        using var db = CreateDbContext();

        AssertTable<DimAdviserEntity>(db, "AFH", "DIM_ADVISER");
        AssertTable<DimCustomerEntity>(db, "AFH", "DIM_CUSTOMER");
        AssertTable<DimCustomerPolicyEntity>(db, "AFH", "DIM_CUSTOMER_X_POLICY");
        AssertTable<DimHouseholdCustomerEntity>(db, "AFH", "DIM_HOUSEHOLD_X_CUST");
        AssertTable<DimPolicyServicePeriodEntity>(db, "AFH", "DIM_POLICY_SERVICE_START_END_DATE");
        AssertTable<DimXplanPolicyEntity>(db, "AFH", "DIM_XPLAN_POLICY");
        AssertTable<FactAumEntity>(db, "AFH", "FACT_AUM");
        AssertTable<FactCustomerEntity>(db, "AFH", "FACT_CUSTOMER");
    }

    [Fact]
    public void Model_MapsImportantSnowflakeColumns()
    {
        using var db = CreateDbContext();

        AssertColumn<DimCustomerEntity>(db, nameof(DimCustomerEntity.ClientEntityId), "CLIENT_ENTITY_ID");
        AssertColumn<FactAumEntity>(db, nameof(FactAumEntity.AdjustedValuation), "ADJUSTED_VALUATION");
        AssertColumn<FactAumEntity>(db, nameof(FactAumEntity.Valuation), "VALUATION");
        AssertColumn<FactCustomerEntity>(db, nameof(FactCustomerEntity.AdviserSk), "ADVISER_SK");
        AssertColumn<DimCustomerPolicyEntity>(db, nameof(DimCustomerPolicyEntity.XplanPolicySk), "XPLAN_POLICY_SK");
    }

    private static AdviserInsightsSnowflakeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AdviserInsightsSnowflakeDbContext>()
            .UseSnowflake("account=test;user=test;password=test;db=DIM_DB_DEV;schema=AFH;warehouse=TEST")
            .Options;

        return new AdviserInsightsSnowflakeDbContext(options);
    }

    private static void AssertTable<TEntity>(
        AdviserInsightsSnowflakeDbContext db,
        string schema,
        string tableName)
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entityType);
        Assert.True(entityType.FindPrimaryKey() is null);
        Assert.Equal(schema, entityType.GetSchema());
        Assert.Equal(tableName, entityType.GetTableName());
    }

    private static void AssertColumn<TEntity>(
        AdviserInsightsSnowflakeDbContext db,
        string propertyName,
        string columnName)
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entityType);
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(columnName, property.GetColumnName());
    }
}
