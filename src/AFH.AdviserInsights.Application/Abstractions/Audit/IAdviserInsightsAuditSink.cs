namespace AFH.AdviserInsights.Application.Abstractions.Audit;

public interface IAdviserInsightsAuditSink
{
    Task WriteAsync(AdviserInsightsAuditEvent auditEvent, CancellationToken cancellationToken);
}
