using AFH.AdviserInsights.Domain.Access;

namespace AFH.AdviserInsights.Application.Abstractions.AI;

public interface ICortexAgentClient
{
    Task<CortexAgentResult> AskAsync(
        string question,
        AdviserDataScope scope,
        string? correlationId,
        CancellationToken cancellationToken);
}
