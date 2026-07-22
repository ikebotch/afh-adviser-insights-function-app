using AFH.AdviserInsights.Application.Abstractions;

namespace AFH.AdviserInsights.Application.Abstractions.Auth;

public interface IIdentityClient
{
    Task<CurrentUserContext?> GetCurrentUserAsync(
        string? bearerToken,
        string? correlationId,
        CancellationToken cancellationToken);
}
