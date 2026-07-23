using AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;
using Microsoft.EntityFrameworkCore;

namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake;

public sealed class AdviserInsightsSnowflakeDbContext(DbContextOptions<AdviserInsightsSnowflakeDbContext> options)
    : DbContext(options)
{
    public DbSet<DimAdviserEntity> Advisers => Set<DimAdviserEntity>();

    public DbSet<DimCustomerEntity> Customers => Set<DimCustomerEntity>();

    public DbSet<DimCustomerPolicyEntity> CustomerPolicies => Set<DimCustomerPolicyEntity>();

    public DbSet<DimHouseholdCustomerEntity> HouseholdCustomers => Set<DimHouseholdCustomerEntity>();

    public DbSet<DimPolicyServicePeriodEntity> PolicyServicePeriods => Set<DimPolicyServicePeriodEntity>();

    public DbSet<DimXplanPolicyEntity> XplanPolicies => Set<DimXplanPolicyEntity>();

    public DbSet<FactAumEntity> AumFacts => Set<FactAumEntity>();

    public DbSet<FactCustomerEntity> CustomerFacts => Set<FactCustomerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DimAdviserEntity>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("DIM_ADVISER", "AFH");
            entity.Property(x => x.AdviserSk).HasColumnName("ADVISER_SK");
            entity.Property(x => x.AdviserId).HasColumnName("ADVISER_ID");
            entity.Property(x => x.Adviser).HasColumnName("ADVISER");
            entity.Property(x => x.EmailAddress).HasColumnName("EMAIL_ADDRESS");
            entity.Property(x => x.AdviserManager).HasColumnName("ADVISER_MANAGER");
            entity.Property(x => x.Region).HasColumnName("REGION");
            entity.Property(x => x.AdviserStatus).HasColumnName("ADVISER_STATUS");
        });

        modelBuilder.Entity<DimCustomerEntity>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("DIM_CUSTOMER", "AFH");
            entity.Property(x => x.EntitySk).HasColumnName("ENTITY_SK");
            entity.Property(x => x.ClientEntityId).HasColumnName("CLIENT_ENTITY_ID");
            entity.Property(x => x.EntityName).HasColumnName("ENTITYNAME");
            entity.Property(x => x.Household).HasColumnName("HOUSEHOLD");
        });

        modelBuilder.Entity<DimCustomerPolicyEntity>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("DIM_CUSTOMER_X_POLICY", "AFH");
            entity.Property(x => x.XplanPolicySk).HasColumnName("XPLAN_POLICY_SK");
            entity.Property(x => x.EntitySk).HasColumnName("ENTITY_SK");
            entity.Property(x => x.ClientEntityId).HasColumnName("CLIENT_ENTITY_ID");
            entity.Property(x => x.ReportName).HasColumnName("REPORT_NAME");
        });

        modelBuilder.Entity<DimHouseholdCustomerEntity>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("DIM_HOUSEHOLD_X_CUST", "AFH");
            entity.Property(x => x.HouseholdSk).HasColumnName("HOUSEHOLD_SK");
            entity.Property(x => x.EntitySk).HasColumnName("ENTITY_SK");
            entity.Property(x => x.ClientEntityId).HasColumnName("CLIENT_ENTITY_ID");
        });

        modelBuilder.Entity<DimPolicyServicePeriodEntity>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("DIM_POLICY_SERVICE_START_END_DATE", "AFH");
            entity.Property(x => x.XplanPolicySk).HasColumnName("XPLAN_POLICY_SK");
            entity.Property(x => x.ReportName).HasColumnName("REPORT_NAME");
            entity.Property(x => x.PolicyStartDate).HasColumnName("POLICY_START_DATE");
            entity.Property(x => x.PolicyServiceCloseDate).HasColumnName("POLICY_SERVICE_CLOSE_DATE");
        });

        modelBuilder.Entity<DimXplanPolicyEntity>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("DIM_XPLAN_POLICY", "AFH");
            entity.Property(x => x.XplanPolicySk).HasColumnName("XPLAN_POLICY_SK");
            entity.Property(x => x.ReportName).HasColumnName("REPORT_NAME");
        });

        modelBuilder.Entity<FactAumEntity>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("FACT_AUM", "AFH");
            entity.Property(x => x.XplanPolicySk).HasColumnName("XPLAN_POLICY_SK");
            entity.Property(x => x.HouseholdSk).HasColumnName("HOUSEHOLD_SK");
            entity.Property(x => x.Valuation).HasColumnName("VALUATION");
            entity.Property(x => x.AdjustedValuation).HasColumnName("ADJUSTED_VALUATION");
        });

        modelBuilder.Entity<FactCustomerEntity>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("FACT_CUSTOMER", "AFH");
            entity.Property(x => x.EntitySk).HasColumnName("ENTITY_SK");
            entity.Property(x => x.HouseholdSk).HasColumnName("HOUSEHOLD_SK");
            entity.Property(x => x.AdviserSk).HasColumnName("ADVISER_SK");
        });
    }
}
