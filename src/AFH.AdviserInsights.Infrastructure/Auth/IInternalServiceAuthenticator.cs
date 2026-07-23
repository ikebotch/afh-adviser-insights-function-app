namespace AFH.AdviserInsights.Infrastructure.Auth;

public interface IInternalServiceAuthenticator
{
    void Apply(HttpRequestMessage request, string? token);
}
