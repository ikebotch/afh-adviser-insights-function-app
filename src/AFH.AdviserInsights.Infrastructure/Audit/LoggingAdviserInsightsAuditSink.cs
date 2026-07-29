using AFH.AdviserInsights.Application.Abstractions.Audit;
using Microsoft.Extensions.Logging;

namespace AFH.AdviserInsights.Infrastructure.Audit;

public sealed class LoggingAdviserInsightsAuditSink(ILogger<LoggingAdviserInsightsAuditSink> logger) : IAdviserInsightsAuditSink
{
    public Task WriteAsync(AdviserInsightsAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Adviser Insights audit {EventId} {OperationName} {Outcome} status={StatusCode} actor={ActorId} correlation={CorrelationId} path={Path} durationMs={DurationMs} failure={FailureReason}",
            auditEvent.EventId,
            auditEvent.OperationName,
            auditEvent.Outcome,
            auditEvent.StatusCode,
            auditEvent.ActorId,
            auditEvent.CorrelationId,
            auditEvent.Path,
            auditEvent.DurationMs,
            auditEvent.FailureReason);

        return Task.CompletedTask;
    }
}
