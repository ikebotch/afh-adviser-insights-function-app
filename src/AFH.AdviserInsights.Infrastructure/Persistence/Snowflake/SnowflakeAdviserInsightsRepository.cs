using AFH.AdviserInsights.Application.Abstractions.Persistence;
using AFH.AdviserInsights.Contract;
using AFH.AdviserInsights.Domain.Access;
using AFH.AdviserInsights.Infrastructure.Persistence.Snowflake.Entities;
using Microsoft.EntityFrameworkCore;

namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake;

public sealed class SnowflakeAdviserInsightsRepository(
    IDbContextFactory<AdviserInsightsSnowflakeDbContext> dbFactory) : IAdviserInsightsRepository
{
    public async Task<AdviserProfileResponse?> GetAdviserProfileAsync(AdviserDataScope scope, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = ApplyAdviserIdentityFilter(db.Advisers.AsNoTracking(), scope);

        return await query
            .Select(adviser => new AdviserProfileResponse(
                adviser.AdviserId ?? string.Empty,
                adviser.Adviser ?? string.Empty,
                adviser.EmailAddress,
                adviser.AdviserManager,
                adviser.Region,
                adviser.AdviserStatus,
                scope.AccessMode))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TeamAdviserResponse>> GetTeamAdvisersAsync(AdviserDataScope scope, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = db.Advisers.AsNoTracking();
        if (!scope.IncludeAll)
        {
            query = query.Where(adviser => adviser.AdviserManager == scope.ManagerName);
        }

        return await query
            .OrderBy(adviser => adviser.Adviser)
            .Take(100)
            .Select(adviser => new TeamAdviserResponse(
                adviser.AdviserId ?? string.Empty,
                adviser.Adviser ?? string.Empty,
                adviser.EmailAddress,
                adviser.AdviserManager,
                adviser.Region,
                adviser.AdviserStatus))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ClientSummaryResponse>> GetClientsAsync(
        AdviserDataScope scope,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query =
            from customer in db.Customers.AsNoTracking()
            join customerFact in db.CustomerFacts.AsNoTracking() on customer.EntitySk equals customerFact.EntitySk
            join adviser in db.Advisers.AsNoTracking() on customerFact.AdviserSk equals adviser.AdviserSk
            join householdCustomer in db.HouseholdCustomers.AsNoTracking() on customer.EntitySk equals householdCustomer.EntitySk into householdCustomers
            from householdCustomer in householdCustomers.DefaultIfEmpty()
            join aumFact in db.AumFacts.AsNoTracking() on householdCustomer.HouseholdSk equals aumFact.HouseholdSk into aumFacts
            from aumFact in aumFacts.DefaultIfEmpty()
            select new AdviserClientAumRow(customer, adviser, aumFact);

        query = ApplyScopeFilter(query, scope);

        var rows = await query
            .GroupBy(row => new
            {
                row.Customer.ClientEntityId,
                row.Customer.EntityName,
                row.Adviser.AdviserId,
                row.Adviser.Adviser,
                row.Customer.Household
            })
            .Select(group => new
            {
                group.Key.ClientEntityId,
                ClientName = group.Key.EntityName,
                group.Key.AdviserId,
                AdviserName = group.Key.Adviser,
                group.Key.Household,
                AumValue = group.Sum(row => row.AumFact == null ? 0m : row.AumFact.AdjustedValuation ?? row.AumFact.Valuation ?? 0m)
            })
            .OrderByDescending(row => row.AumValue)
            .ThenBy(row => row.ClientName)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(row => new ClientSummaryResponse(
            row.ClientEntityId ?? string.Empty,
            row.ClientName,
            row.AdviserId,
            row.AdviserName,
            row.Household,
            row.AumValue)).ToArray();
    }

    public async Task<IReadOnlyList<PolicySummaryResponse>> GetPoliciesAsync(
        AdviserDataScope scope,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query =
            from customer in db.Customers.AsNoTracking()
            join customerFact in db.CustomerFacts.AsNoTracking() on customer.EntitySk equals customerFact.EntitySk
            join adviser in db.Advisers.AsNoTracking() on customerFact.AdviserSk equals adviser.AdviserSk
            join customerPolicy in db.CustomerPolicies.AsNoTracking() on customer.EntitySk equals customerPolicy.EntitySk
            join policy in db.XplanPolicies.AsNoTracking() on customerPolicy.XplanPolicySk equals policy.XplanPolicySk
            join servicePeriod in db.PolicyServicePeriods.AsNoTracking() on policy.XplanPolicySk equals servicePeriod.XplanPolicySk into servicePeriods
            from servicePeriod in servicePeriods.DefaultIfEmpty()
            join aumFact in db.AumFacts.AsNoTracking() on policy.XplanPolicySk equals aumFact.XplanPolicySk into aumFacts
            from aumFact in aumFacts.DefaultIfEmpty()
            select new AdviserPolicyAumRow(customer, adviser, policy, servicePeriod, aumFact);

        query = ApplyScopeFilter(query, scope);

        var rows = await query
            .GroupBy(row => new
            {
                row.Policy.XplanPolicySk,
                row.Customer.ClientEntityId,
                row.Customer.EntityName,
                row.Adviser.AdviserId,
                row.Adviser.Adviser,
                ReportName = row.ServicePeriod == null ? row.Policy.ReportName : row.ServicePeriod.ReportName ?? row.Policy.ReportName,
                PolicyStartDate = row.ServicePeriod == null ? null : row.ServicePeriod.PolicyStartDate,
                PolicyServiceCloseDate = row.ServicePeriod == null ? null : row.ServicePeriod.PolicyServiceCloseDate
            })
            .Select(group => new
            {
                PolicyId = group.Key.XplanPolicySk,
                group.Key.ClientEntityId,
                ClientName = group.Key.EntityName,
                group.Key.AdviserId,
                AdviserName = group.Key.Adviser,
                group.Key.ReportName,
                group.Key.PolicyStartDate,
                group.Key.PolicyServiceCloseDate,
                AumValue = group.Sum(row => row.AumFact == null ? 0m : row.AumFact.AdjustedValuation ?? row.AumFact.Valuation ?? 0m)
            })
            .OrderByDescending(row => row.AumValue)
            .ThenBy(row => row.ClientName)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(row => new PolicySummaryResponse(
            row.PolicyId ?? string.Empty,
            row.ClientEntityId,
            row.ClientName,
            row.AdviserId,
            row.AdviserName,
            row.ReportName,
            ToDateOnly(row.PolicyStartDate),
            ToDateOnly(row.PolicyServiceCloseDate),
            row.AumValue)).ToArray();
    }

    public async Task<AumSummaryResponse> GetAumSummaryAsync(AdviserDataScope scope, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query =
            from customer in db.Customers.AsNoTracking()
            join customerFact in db.CustomerFacts.AsNoTracking() on customer.EntitySk equals customerFact.EntitySk
            join adviser in db.Advisers.AsNoTracking() on customerFact.AdviserSk equals adviser.AdviserSk
            join householdCustomer in db.HouseholdCustomers.AsNoTracking() on customer.EntitySk equals householdCustomer.EntitySk into householdCustomers
            from householdCustomer in householdCustomers.DefaultIfEmpty()
            join aumFact in db.AumFacts.AsNoTracking() on householdCustomer.HouseholdSk equals aumFact.HouseholdSk into aumFacts
            from aumFact in aumFacts.DefaultIfEmpty()
            select new AdviserClientAumRow(customer, adviser, aumFact);

        query = ApplyScopeFilter(query, scope);

        var summary = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                AdviserCount = group.Select(row => row.Adviser.AdviserId).Distinct().Count(),
                ClientCount = group.Select(row => row.Customer.ClientEntityId).Distinct().Count(),
                PolicyCount = group.Select(row => row.AumFact == null ? null : row.AumFact.XplanPolicySk).Distinct().Count(),
                TotalAum = group.Sum(row => row.AumFact == null ? 0m : row.AumFact.AdjustedValuation ?? row.AumFact.Valuation ?? 0m)
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AumSummaryResponse(
            scope.AccessMode,
            summary?.AdviserCount ?? 0,
            summary?.ClientCount ?? 0,
            summary?.PolicyCount ?? 0,
            summary?.TotalAum ?? 0m);
    }

    public async Task<IReadOnlyList<HighValueClientResponse>> GetHighValueClientsAsync(
        AdviserDataScope scope,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query =
            from customer in db.Customers.AsNoTracking()
            join customerFact in db.CustomerFacts.AsNoTracking() on customer.EntitySk equals customerFact.EntitySk
            join adviser in db.Advisers.AsNoTracking() on customerFact.AdviserSk equals adviser.AdviserSk
            join householdCustomer in db.HouseholdCustomers.AsNoTracking() on customer.EntitySk equals householdCustomer.EntitySk into householdCustomers
            from householdCustomer in householdCustomers.DefaultIfEmpty()
            join aumFact in db.AumFacts.AsNoTracking() on householdCustomer.HouseholdSk equals aumFact.HouseholdSk into aumFacts
            from aumFact in aumFacts.DefaultIfEmpty()
            select new AdviserClientAumRow(customer, adviser, aumFact);

        query = ApplyScopeFilter(query, scope);

        var rows = await query
            .GroupBy(row => new
            {
                row.Customer.ClientEntityId,
                row.Customer.EntityName,
                row.Adviser.AdviserId,
                row.Adviser.Adviser
            })
            .Select(group => new
            {
                group.Key.ClientEntityId,
                ClientName = group.Key.EntityName,
                group.Key.AdviserId,
                AdviserName = group.Key.Adviser,
                TotalPolicyValue = group.Sum(row => row.AumFact == null ? 0m : row.AumFact.AdjustedValuation ?? row.AumFact.Valuation ?? 0m),
                PolicyCount = group.Select(row => row.AumFact == null ? null : row.AumFact.XplanPolicySk).Distinct().Count()
            })
            .OrderByDescending(row => row.TotalPolicyValue)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(row => new HighValueClientResponse(
            row.ClientEntityId ?? string.Empty,
            row.ClientName,
            row.AdviserId,
            row.AdviserName,
            row.TotalPolicyValue,
            row.PolicyCount)).ToArray();
    }

    public async Task<IReadOnlyList<MissingAnnualReviewClientResponse>> GetClientsMissingAnnualReviewAsync(
        AdviserDataScope scope,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query =
            from customer in db.Customers.AsNoTracking()
            join customerFact in db.CustomerFacts.AsNoTracking() on customer.EntitySk equals customerFact.EntitySk
            join adviser in db.Advisers.AsNoTracking() on customerFact.AdviserSk equals adviser.AdviserSk
            join customerPolicy in db.CustomerPolicies.AsNoTracking() on customer.EntitySk equals customerPolicy.EntitySk
            join policy in db.XplanPolicies.AsNoTracking() on customerPolicy.XplanPolicySk equals policy.XplanPolicySk
            join servicePeriod in db.PolicyServicePeriods.AsNoTracking() on policy.XplanPolicySk equals servicePeriod.XplanPolicySk into servicePeriods
            from servicePeriod in servicePeriods.DefaultIfEmpty()
            select new AdviserPolicyServiceRow(customer, adviser, policy, servicePeriod);

        query = ApplyScopeFilter(query, scope);

        var cutoff = DateTime.UtcNow.Date.AddYears(-1);
        var rows = await query
            .GroupBy(row => new
            {
                row.Customer.ClientEntityId,
                row.Customer.EntityName,
                row.Adviser.AdviserId,
                row.Adviser.Adviser
            })
            .Select(group => new
            {
                group.Key.ClientEntityId,
                ClientName = group.Key.EntityName,
                group.Key.AdviserId,
                AdviserName = group.Key.Adviser,
                LastPolicyServiceDate = group.Max(row => row.ServicePeriod == null ? null : row.ServicePeriod.PolicyServiceCloseDate),
                ActivePolicyCount = group.Select(row => row.Policy.XplanPolicySk).Distinct().Count()
            })
            .Where(row => row.LastPolicyServiceDate == null || row.LastPolicyServiceDate < cutoff)
            .OrderBy(row => row.LastPolicyServiceDate)
            .ThenBy(row => row.ClientName)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(row => new MissingAnnualReviewClientResponse(
            row.ClientEntityId ?? string.Empty,
            row.ClientName,
            row.AdviserId,
            row.AdviserName,
            ToDateOnly(row.LastPolicyServiceDate),
            row.ActivePolicyCount)).ToArray();
    }

    private static IQueryable<DimAdviserEntity> ApplyAdviserIdentityFilter(
        IQueryable<DimAdviserEntity> query,
        AdviserDataScope scope)
    {
        var email = scope.Email?.ToLower();
        return query.Where(adviser =>
            adviser.AdviserId == scope.AdviserId ||
            (adviser.EmailAddress != null && adviser.EmailAddress.ToLower() == email) ||
            adviser.Adviser == scope.ManagerName);
    }

    private static IQueryable<AdviserClientAumRow> ApplyScopeFilter(
        IQueryable<AdviserClientAumRow> query,
        AdviserDataScope scope)
    {
        if (scope.IncludeAll)
            return query;

        var email = scope.Email?.ToLower();
        return scope.IncludeTeam
            ? query.Where(row =>
                row.Adviser.AdviserManager == scope.ManagerName ||
                row.Adviser.AdviserId == scope.AdviserId ||
                (row.Adviser.EmailAddress != null && row.Adviser.EmailAddress.ToLower() == email))
            : query.Where(row =>
                row.Adviser.AdviserId == scope.AdviserId ||
                (row.Adviser.EmailAddress != null && row.Adviser.EmailAddress.ToLower() == email) ||
                row.Adviser.Adviser == scope.ManagerName);
    }

    private static IQueryable<AdviserPolicyAumRow> ApplyScopeFilter(
        IQueryable<AdviserPolicyAumRow> query,
        AdviserDataScope scope)
    {
        if (scope.IncludeAll)
            return query;

        var email = scope.Email?.ToLower();
        return scope.IncludeTeam
            ? query.Where(row =>
                row.Adviser.AdviserManager == scope.ManagerName ||
                row.Adviser.AdviserId == scope.AdviserId ||
                (row.Adviser.EmailAddress != null && row.Adviser.EmailAddress.ToLower() == email))
            : query.Where(row =>
                row.Adviser.AdviserId == scope.AdviserId ||
                (row.Adviser.EmailAddress != null && row.Adviser.EmailAddress.ToLower() == email) ||
                row.Adviser.Adviser == scope.ManagerName);
    }

    private static IQueryable<AdviserPolicyServiceRow> ApplyScopeFilter(
        IQueryable<AdviserPolicyServiceRow> query,
        AdviserDataScope scope)
    {
        if (scope.IncludeAll)
            return query;

        var email = scope.Email?.ToLower();
        return scope.IncludeTeam
            ? query.Where(row =>
                row.Adviser.AdviserManager == scope.ManagerName ||
                row.Adviser.AdviserId == scope.AdviserId ||
                (row.Adviser.EmailAddress != null && row.Adviser.EmailAddress.ToLower() == email))
            : query.Where(row =>
                row.Adviser.AdviserId == scope.AdviserId ||
                (row.Adviser.EmailAddress != null && row.Adviser.EmailAddress.ToLower() == email) ||
                row.Adviser.Adviser == scope.ManagerName);
    }

    private static DateOnly? ToDateOnly(DateTime? value)
        => value is null ? null : DateOnly.FromDateTime(value.Value);

    private sealed record AdviserClientAumRow(
        DimCustomerEntity Customer,
        DimAdviserEntity Adviser,
        FactAumEntity? AumFact);

    private sealed record AdviserPolicyAumRow(
        DimCustomerEntity Customer,
        DimAdviserEntity Adviser,
        DimXplanPolicyEntity Policy,
        DimPolicyServicePeriodEntity? ServicePeriod,
        FactAumEntity? AumFact);

    private sealed record AdviserPolicyServiceRow(
        DimCustomerEntity Customer,
        DimAdviserEntity Adviser,
        DimXplanPolicyEntity Policy,
        DimPolicyServicePeriodEntity? ServicePeriod);
}
