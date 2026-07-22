using System.Net;
using AFH.AdviserInsights.Application.Abstractions;

namespace AFH.AdviserInsights.Application.Services;

public sealed class AdviserInsightsAccessService(IIdentityClient identityClient)
{
    public async Task<ServiceResult<AdviserDataScope>> ResolveScopeAsync(
        string? bearerToken,
        string? correlationId,
        bool allowTeamScope,
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
                IncludeAll: true));
        }

        if (allowTeamScope && HasPermission(user, AdviserInsightPermissionNames.TeamRead))
        {
            return ServiceResult<AdviserDataScope>.Ok(new AdviserDataScope(
                "Team",
                user.Email,
                user.AdviserId,
                user.DisplayName,
                IncludeTeam: true,
                IncludeAll: false));
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
                IncludeAll: false));
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
