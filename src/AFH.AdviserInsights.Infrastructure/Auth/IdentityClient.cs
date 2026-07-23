using System.Net.Http.Json;
using System.Text.Json;
using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Application.Abstractions.Auth;
using AFH.AdviserInsights.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Infrastructure.Auth;

public sealed class IdentityClient : IIdentityClient
{
    private const string UserBearerTokenHeaderName = "x-afh-user-token";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly AdviserInsightsOptions _options;
    private readonly IInternalServiceAuthenticator _authenticator;
    private readonly ILogger<IdentityClient> _logger;

    public IdentityClient(
        HttpClient http,
        IOptions<AdviserInsightsOptions> options,
        IInternalServiceAuthenticator authenticator,
        ILogger<IdentityClient> logger)
    {
        _http = http;
        _options = options.Value;
        _authenticator = authenticator;
        _logger = logger;
    }

    public async Task<CurrentUserContext?> GetCurrentUserAsync(
        string? bearerToken,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var identity = _options.Identity;
        if (!Uri.TryCreate(identity.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            _logger.LogWarning("Identity base URL is not configured.");
            return null;
        }

        var currentUserPath = string.IsNullOrWhiteSpace(identity.CurrentUserPath)
            ? "api/internal/identity/v1/me"
            : identity.CurrentUserPath.TrimStart('/');

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, currentUserPath));

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.TryAddWithoutValidation(UserBearerTokenHeaderName, bearerToken.Trim());
        if (!string.IsNullOrWhiteSpace(correlationId))
            request.Headers.TryAddWithoutValidation("x-correlation-id", correlationId);
        if (!string.IsNullOrWhiteSpace(identity.FunctionKey))
            request.Headers.TryAddWithoutValidation("x-functions-key", identity.FunctionKey.Trim());

        _authenticator.Apply(request, identity.InternalToken);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Identity current-user request failed with HTTP {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            var envelope = await response.Content.ReadFromJsonAsync<IdentityCurrentUserEnvelope>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            var user = envelope?.Data ?? envelope?.AsUser();
            return user is null
                ? null
                : new CurrentUserContext(
                    user.UserProfileId ?? user.UserId,
                    user.ExternalSubject,
                    user.Email,
                    user.DisplayName,
                    user.AdviserId,
                    user.Roles ?? [],
                    user.Permissions ?? [],
                    user.AccessScopes?.Select(scope => new UserAccessScope(scope.Area, scope.ScopeType, scope.ScopeValue, scope.DisplayName)).ToArray() ?? []);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Identity current-user request failed.");
            return null;
        }
    }

    private sealed record IdentityCurrentUserEnvelope(
        bool? Success,
        IdentityCurrentUser? Data,
        string? UserProfileId,
        string? UserId,
        string? ExternalSubject,
        string? Email,
        string? DisplayName,
        string? JobRole,
        string? AdviserId,
        IReadOnlyList<string>? Roles,
        IReadOnlyList<string>? Permissions,
        IReadOnlyList<IdentityAccessScope>? AccessScopes)
    {
        public IdentityCurrentUser? AsUser()
        {
            if (Data is not null)
                return Data;

            var hasUserShape = UserProfileId is not null ||
                               UserId is not null ||
                               ExternalSubject is not null ||
                               Email is not null ||
                               DisplayName is not null ||
                               JobRole is not null ||
                               AdviserId is not null ||
                               Roles is not null ||
                               Permissions is not null ||
                               AccessScopes is not null;

            return hasUserShape
                ? new IdentityCurrentUser(UserProfileId, UserId, ExternalSubject, Email, DisplayName, JobRole, AdviserId, Roles, Permissions, AccessScopes)
                : null;
        }
    }

    private sealed record IdentityCurrentUser(
        string? UserProfileId,
        string? UserId,
        string? ExternalSubject,
        string? Email,
        string? DisplayName,
        string? JobRole,
        string? AdviserId,
        IReadOnlyList<string>? Roles,
        IReadOnlyList<string>? Permissions,
        IReadOnlyList<IdentityAccessScope>? AccessScopes);

    private sealed record IdentityAccessScope(
        string Area,
        string ScopeType,
        string? ScopeValue,
        string? DisplayName);
}
