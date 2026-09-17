using System.Net;
using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Application.Abstractions.AI;
using AFH.AdviserInsights.Application.Abstractions.Persistence;
using AFH.AdviserInsights.Contract;
using AFH.AdviserInsights.Domain.Access;

namespace AFH.AdviserInsights.Application.Services;

public sealed class AdviserInsightsService(
    AdviserInsightsAccessService accessService,
    IAdviserInsightsRepository repository,
    ICortexAgentClient cortexAgentClient)
{
    private const int MaxFailureDetailLength = 4096;

    public async Task<ServiceResult<AdviserProfileResponse>> GetMyAdviserAsync(
        string? bearerToken,
        string? correlationId,
        AdviserTargetFilter? target,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: target is not null, target, cancellationToken)
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
        AdviserTargetFilter? target,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, target, cancellationToken)
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
        AdviserTargetFilter? target,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, target, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<IReadOnlyList<ClientSummaryResponse>>(scope);

        return ServiceResult<IReadOnlyList<ClientSummaryResponse>>.Ok(
            await repository.GetClientsAsync(scope.Value!, NormalizePageSize(pageSize), cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<IReadOnlyList<PolicySummaryResponse>>> GetMyPoliciesAsync(
        string? bearerToken,
        string? correlationId,
        AdviserTargetFilter? target,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, target, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<IReadOnlyList<PolicySummaryResponse>>(scope);

        return ServiceResult<IReadOnlyList<PolicySummaryResponse>>.Ok(
            await repository.GetPoliciesAsync(scope.Value!, NormalizePageSize(pageSize), cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<AumSummaryResponse>> GetMyAumSummaryAsync(
        string? bearerToken,
        string? correlationId,
        AdviserTargetFilter? target,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, target, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<AumSummaryResponse>(scope);

        return ServiceResult<AumSummaryResponse>.Ok(
            await repository.GetAumSummaryAsync(scope.Value!, cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<IReadOnlyList<HighValueClientResponse>>> GetMyHighValueClientsAsync(
        string? bearerToken,
        string? correlationId,
        AdviserTargetFilter? target,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, target, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<IReadOnlyList<HighValueClientResponse>>(scope);

        return ServiceResult<IReadOnlyList<HighValueClientResponse>>.Ok(
            await repository.GetHighValueClientsAsync(scope.Value!, NormalizePageSize(pageSize), cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<IReadOnlyList<MissingAnnualReviewClientResponse>>> GetMyClientsMissingAnnualReviewAsync(
        string? bearerToken,
        string? correlationId,
        AdviserTargetFilter? target,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(bearerToken, correlationId, allowTeamScope: true, target, cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<IReadOnlyList<MissingAnnualReviewClientResponse>>(scope);

        return ServiceResult<IReadOnlyList<MissingAnnualReviewClientResponse>>.Ok(
            await repository.GetClientsMissingAnnualReviewAsync(scope.Value!, NormalizePageSize(pageSize), cancellationToken).ConfigureAwait(false));
    }

    public async Task<ServiceResult<CortexAnswerResponse>> AskCortexAgentAsync(
        string? bearerToken,
        string? correlationId,
        string? question,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return ServiceResult<CortexAnswerResponse>.Fail(
                HttpStatusCode.BadRequest,
                "ValidationError",
                "Question is required.");
        }

        var scope = await ResolveScopeAsync(
                bearerToken,
                correlationId,
                allowTeamScope: true,
                target: null,
                cancellationToken)
            .ConfigureAwait(false);
        if (!scope.IsSuccess)
            return Fail<CortexAnswerResponse>(scope);

        var result = await cortexAgentClient
            .AskAsync(question.Trim(), scope.Value!, correlationId, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var detail = result.Content.GetRawText();
            return ServiceResult<CortexAnswerResponse>.Fail(
                HttpStatusCode.BadGateway,
                "CortexAgentError",
                $"Snowflake Cortex agent returned HTTP {result.StatusCode}.",
                detail.Length <= MaxFailureDetailLength ? detail : detail[..MaxFailureDetailLength]);
        }

        return ServiceResult<CortexAnswerResponse>.Ok(
            new CortexAnswerResponse(result.Content, scope.Value!.AccessMode));
    }

    private Task<ServiceResult<AdviserDataScope>> ResolveScopeAsync(
        string? bearerToken,
        string? correlationId,
        bool allowTeamScope,
        AdviserTargetFilter? target,
        CancellationToken cancellationToken)
        => accessService.ResolveScopeAsync(bearerToken, correlationId, allowTeamScope, target, cancellationToken);

    private static ServiceResult<T> Fail<T>(ServiceResult<AdviserDataScope> scope)
        => ServiceResult<T>.Fail(scope.StatusCode, scope.ErrorCode!, scope.ErrorMessage!, scope.ErrorDetail);

    private static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize <= 0 ? 25 : pageSize, 1, 100);
}
