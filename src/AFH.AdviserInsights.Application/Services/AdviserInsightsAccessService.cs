using System.Net;
using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Application.Abstractions.Auth;
using AFH.AdviserInsights.Domain.Access;

namespace AFH.AdviserInsights.Application.Services;

public sealed class AdviserInsightsAccessService(IIdentityClient identityClient)
{
    public async Task<ServiceResult<AdviserDataScope>> ResolveScopeAsync(
        string? bearerToken,
        string? correlationId,
        bool allowTeamScope,
        AdviserTargetFilter? target,
        CancellationToken cancellationToken)
    {
        var user = await identityClient.GetCurrentUserAsync(bearerToken, correlationId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return ServiceResult<AdviserDataScope>.Fail(
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Unable to resolve current user permissions.");
        }

        if (HasPermission(user, AdviserInsightPermissionNames.AllRead))
        {
            return ServiceResult<AdviserDataScope>.Ok(new AdviserDataScope(
                "All",
                user.Email,
                user.AdviserId,
                user.DisplayName,
                IncludeTeam: true,
                IncludeAll: true,
                TargetAdviserId: target?.AdviserId,
                TargetAdviserName: target?.AdviserName,
                TargetAdviserEmail: target?.AdviserEmail));
        }

        if (allowTeamScope && HasPermission(user, AdviserInsightPermissionNames.TeamRead))
        {
            return ServiceResult<AdviserDataScope>.Ok(new AdviserDataScope(
                "Team",
                user.Email,
                user.AdviserId,
                user.DisplayName,
                IncludeTeam: true,
                IncludeAll: false,
                TargetAdviserId: target?.AdviserId,
                TargetAdviserName: target?.AdviserName,
                TargetAdviserEmail: target?.AdviserEmail));
        }

        if (HasPermission(user, AdviserInsightPermissionNames.SelfRead) ||
            !string.IsNullOrWhiteSpace(user.AdviserId) ||
            HasAdviserSelfScope(user))
        {
            return ServiceResult<AdviserDataScope>.Ok(new AdviserDataScope(
                "Self",
                user.Email,
                user.AdviserId,
                user.DisplayName,
                IncludeTeam: false,
                IncludeAll: false,
                TargetAdviserId: target?.AdviserId,
                TargetAdviserName: target?.AdviserName,
                TargetAdviserEmail: target?.AdviserEmail));
        }

        return ServiceResult<AdviserDataScope>.Fail(
            HttpStatusCode.Forbidden,
            "Forbidden",
            "AUM adviser insight permission is required.");
    }

    private static bool HasPermission(CurrentUserContext user, string permission)
        => user.Permissions.Contains("*", StringComparer.OrdinalIgnoreCase) ||
           user.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    private static bool HasAdviserSelfScope(CurrentUserContext user)
        => user.AccessScopes.Any(scope =>
            IsAumArea(scope.Area) &&
            scope.ScopeType.Equals("AdviserSelf", StringComparison.OrdinalIgnoreCase));

    private static bool IsAumArea(string area)
        => area.Equals("*", StringComparison.OrdinalIgnoreCase) ||
           area.Equals("AUM", StringComparison.OrdinalIgnoreCase) ||
           area.Equals("Aum", StringComparison.OrdinalIgnoreCase) ||
           area.Equals("AdviserInsights", StringComparison.OrdinalIgnoreCase);
}

public sealed record AdviserTargetFilter(
    string? AdviserId,
    string? AdviserName,
    string? AdviserEmail);
