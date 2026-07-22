namespace AFH.AdviserInsights.Application.Abstractions;

public interface IIdentityClient
{
    Task<CurrentUserContext?> GetCurrentUserAsync(
        string? bearerToken,
        string? correlationId,
        CancellationToken cancellationToken);
}
