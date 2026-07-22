using System.Net;
using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Contract;

namespace AFH.AdviserInsights.Application.Services;

public sealed class AdviserInsightsService(
    AdviserInsightsAccessService accessService,
    IAdviserInsightsRepository repository)
{
    public async Task<ServiceResult<AdviserProfileResponse>> GetMyAdviserAsync(
        string? bearerToken,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: false, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<AdviserProfileResponse>(scope);

        var profile = await repository.GetAdviserProfileAsync(scope.Value!, cancellationToken).ConfigureAwait(false);
        return profile is null
            ? ServiceResult<AdviserProfileResponse>.Fail(HttpStatusCode.NotFound, "NotFound", "No adviser profile was found for the signed-in user.")
            : ServiceResult<AdviserProfileResponse>.Ok(profile);
    }

    public async Task<ServiceResult<IReadOnlyList<TeamAdviserResponse>>> GetMyTeamAdvisersAsync(
        string? bearerToken,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<IReadOnlyList<TeamAdviserResponse>>(scope);

        if (!scope.Value!.IncludeTeam && !scope.Value.IncludeAll)
            return ServiceResult<IReadOnlyList<TeamAdviserResponse>>.Ok([]);

        return ServiceResult<IReadOnlyList<TeamAdviserResponse>>.Ok(
            await repository.GetTeamAdvisersAsync(scope.Value, cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<IReadOnlyList<ClientSummaryResponse>>> GetMyClientsAsync(
        string? bearerToken,
        string? correlationId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<IReadOnlyList<ClientSummaryResponse>>(scope);

        return ServiceResult<IReadOnlyList<ClientSummaryResponse>>.Ok(
            await repository.GetClientsAsync(scope.Value!, NormalizePageSize(pageSize), cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<IReadOnlyList<PolicySummaryResponse>>> GetMyPoliciesAsync(
        string? bearerToken,
        string? correlationId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<IReadOnlyList<PolicySummaryResponse>>(scope);

        return ServiceResult<IReadOnlyList<PolicySummaryResponse>>.Ok(
            await repository.GetPoliciesAsync(scope.Value!, NormalizePageSize(pageSize), cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<AumSummaryResponse>> GetMyAumSummaryAsync(
        string? bearerToken,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<AumSummaryResponse>(scope);

        return ServiceResult<AumSummaryResponse>.Ok(
            await repository.GetAumSummaryAsync(scope.Value!, cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<IReadOnlyList<HighValueClientResponse>>> GetMyHighValueClientsAsync(
        string? bearerToken,
        string? correlationId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<IReadOnlyList<HighValueClientResponse>>(scope);

        return ServiceResult<IReadOnlyList<HighValueClientResponse>>.Ok(
            await repository.GetHighValueClientsAsync(scope.Value!, NormalizePageSize(pageSize), cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<IReadOnlyList<MissingAnnualReviewClientResponse>>> GetMyClientsMissingAnnualReviewAsync(
        string? bearerToken,
        string? correlationId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<IReadOnlyList<MissingAnnualReviewClientResponse>>(scope);

        return ServiceResult<IReadOnlyList<MissingAnnualReviewClientResponse>>.Ok(
            await repository.GetClientsMissingAnnualReviewAsync(scope.Value!, NormalizePageSize(pageSize), cancellationToken).ConfigureAwait(false));
    }

    private Task<ServiceResult<AdviserDataScope>> ResolveScopeAsync(
        string? bearerToken,
        string? correlationId,
        bool allowTeamScope,
        CancellationToken cancellationToken)
        => accessService.ResolveScopeAsync(bearerToken, correlationId, allowTeamScope, cancellationToken);

    private static ServiceResult<T> Fail<T>(ServiceResult<AdviserDataScope> scope)
        => ServiceResult<T>.Fail(scope.StatusCode, scope.ErrorCode!, scope.ErrorMessage!, scope.ErrorDetail);

    private static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize <= 0 ? 25 : pageSize, 1, 100);
}
