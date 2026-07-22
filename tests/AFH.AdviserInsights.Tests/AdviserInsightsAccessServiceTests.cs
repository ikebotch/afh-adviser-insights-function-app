using System.Net;
using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Application.Abstractions.Auth;
using AFH.AdviserInsights.Application.Services;

namespace AFH.AdviserInsights.Tests;

public sealed class AdviserInsightsAccessServiceTests
{
    [Fact]
    public async Task ResolveScopeAsync_WhenUserHasSelfPermission_ReturnsSelfScope()
    {
        var identity = new StubIdentityClient(new CurrentUserContext(
            "user-1",
            "subject-1",
            "adviser@afh.com",
            "Adviser One",
            "adv-1",
            ["Adviser"],
            [AdviserInsightPermissionNames.SelfRead],
            []));
        var service = new AdviserInsightsAccessService(identity);

        var result = await service.ResolveScopeAsync("Bearer token", "corr-1", allowTeamScope: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Self", result.Value!.AccessMode);
        Assert.False(result.Value.IncludeTeam);
        Assert.Equal("adv-1", result.Value.AdviserId);
    }

    [Fact]
    public async Task ResolveScopeAsync_WhenUserHasTeamPermission_ReturnsTeamScope()
    {
        var identity = new StubIdentityClient(new CurrentUserContext(
            "user-1",
            "subject-1",
            "manager@afh.com",
            "Manager One",
            "mgr-1",
            ["Manager"],
            [AdviserInsightPermissionNames.TeamRead],
            []));
        var service = new AdviserInsightsAccessService(identity);

        var result = await service.ResolveScopeAsync("Bearer token", "corr-1", allowTeamScope: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Team", result.Value!.AccessMode);
        Assert.True(result.Value.IncludeTeam);
        Assert.Equal("Manager One", result.Value.ManagerName);
    }

    [Fact]
    public async Task ResolveScopeAsync_WhenIdentityMissing_ReturnsUnauthorized()
    {
        var service = new AdviserInsightsAccessService(new StubIdentityClient(null));

        var result = await service.ResolveScopeAsync("Bearer token", "corr-1", allowTeamScope: true, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("Unable to resolve current user permissions.", result.ErrorMessage);
    }

    private sealed class StubIdentityClient(CurrentUserContext? user) : IIdentityClient
    {
        public Task<CurrentUserContext?> GetCurrentUserAsync(string? bearerToken, string? correlationId, CancellationToken cancellationToken)
            => Task.FromResult(user);
    }
}
