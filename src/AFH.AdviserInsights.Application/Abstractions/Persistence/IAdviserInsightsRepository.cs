using AFH.AdviserInsights.Contract;
using AFH.AdviserInsights.Domain.Access;

namespace AFH.AdviserInsights.Application.Abstractions.Persistence;

public interface IAdviserInsightsRepository
{
    Task<AdviserProfileResponse?> GetAdviserProfileAsync(AdviserDataScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamAdviserResponse>> GetTeamAdvisersAsync(AdviserDataScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClientSummaryResponse>> GetClientsAsync(AdviserDataScope scope, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<PolicySummaryResponse>> GetPoliciesAsync(AdviserDataScope scope, int pageSize, CancellationToken cancellationToken);

    Task<AumSummaryResponse> GetAumSummaryAsync(AdviserDataScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyList<HighValueClientResponse>> GetHighValueClientsAsync(AdviserDataScope scope, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<MissingAnnualReviewClientResponse>> GetClientsMissingAnnualReviewAsync(AdviserDataScope scope, int pageSize, CancellationToken cancellationToken);
}
