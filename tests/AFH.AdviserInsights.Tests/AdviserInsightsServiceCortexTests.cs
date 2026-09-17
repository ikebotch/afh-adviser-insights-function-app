using System.Net;
using System.Text.Json;
using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Application.Abstractions.AI;
using AFH.AdviserInsights.Application.Abstractions.Auth;
using AFH.AdviserInsights.Application.Abstractions.Persistence;
using AFH.AdviserInsights.Application.Services;
using AFH.AdviserInsights.Contract;
using AFH.AdviserInsights.Domain.Access;

namespace AFH.AdviserInsights.Tests;

public sealed class AdviserInsightsServiceCortexTests
{
    [Fact]
    public async Task AskCortexAgentAsync_WhenIdentityCannotBeResolved_DoesNotCallCortex()
    {
        var cortex = new StubCortexAgentClient();
        var service = CreateService(null, cortex);

        var result = await service.AskCortexAgentAsync(
            "user-token",
            "corr-1",
            "Show advisers",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal(0, cortex.CallCount);
    }

    [Fact]
    public async Task AskCortexAgentAsync_WhenManagerIsAuthorized_PassesTeamScopeToCortex()
    {
        var user = new CurrentUserContext(
            "manager-1",
            "subject-1",
            "manager@afh.com",
            "Manager One",
            "manager-adviser-id",
            ["Manager"],
            [AdviserInsightPermissionNames.TeamRead],
            []);
        var cortex = new StubCortexAgentClient
        {
            Result = new CortexAgentResult(
                true,
                200,
                JsonSerializer.SerializeToElement(new { answer = "Five advisers" }))
        };
        var service = CreateService(user, cortex);

        var result = await service.AskCortexAgentAsync(
            "user-token",
            "corr-1",
            "Show advisers",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Team", result.Value!.AccessMode);
        Assert.Equal("Team", cortex.Scope!.AccessMode);
        Assert.Equal("Manager One", cortex.Scope.ManagerName);
        Assert.Equal("Show advisers", cortex.Question);
    }

    private static AdviserInsightsService CreateService(
        CurrentUserContext? user,
        StubCortexAgentClient cortex)
    {
        var access = new AdviserInsightsAccessService(new StubIdentityClient(user));
        return new AdviserInsightsService(access, new UnusedRepository(), cortex);
    }

    private sealed class StubIdentityClient(CurrentUserContext? user) : IIdentityClient
    {
        public Task<CurrentUserContext?> GetCurrentUserAsync(
            string? bearerToken,
            string? correlationId,
            CancellationToken cancellationToken)
            => Task.FromResult(user);
    }

    private sealed class StubCortexAgentClient : ICortexAgentClient
    {
        public int CallCount { get; private set; }

        public string? Question { get; private set; }

        public AdviserDataScope? Scope { get; private set; }

        public CortexAgentResult Result { get; init; } = new(
            true,
            200,
            JsonSerializer.SerializeToElement(new { }));

        public Task<CortexAgentResult> AskAsync(
            string question,
            AdviserDataScope scope,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Question = question;
            Scope = scope;
            return Task.FromResult(Result);
        }
    }

    private sealed class UnusedRepository : IAdviserInsightsRepository
    {
        public Task<AdviserProfileResponse?> GetAdviserProfileAsync(AdviserDataScope scope, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TeamAdviserResponse>> GetTeamAdvisersAsync(AdviserDataScope scope, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<ClientSummaryResponse>> GetClientsAsync(AdviserDataScope scope, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PolicySummaryResponse>> GetPoliciesAsync(AdviserDataScope scope, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AumSummaryResponse> GetAumSummaryAsync(AdviserDataScope scope, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<HighValueClientResponse>> GetHighValueClientsAsync(AdviserDataScope scope, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MissingAnnualReviewClientResponse>> GetClientsMissingAnnualReviewAsync(AdviserDataScope scope, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
