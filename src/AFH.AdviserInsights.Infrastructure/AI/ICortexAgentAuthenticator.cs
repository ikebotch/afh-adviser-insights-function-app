namespace AFH.AdviserInsights.Infrastructure.AI;

public interface ICortexAgentAuthenticator
{
    void Apply(HttpRequestMessage request);
}
