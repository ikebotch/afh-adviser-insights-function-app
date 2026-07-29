namespace AFH.AdviserInsights.Application.Abstractions.Audit;

public sealed record AdviserInsightsAuditEvent(
    string EventId,
    DateTimeOffset OccurredUtc,
    string OperationName,
    string Method,
    string Path,
    string? QueryString,
    string Outcome,
    int StatusCode,
    string? CorrelationId,
    string? ActorId,
    long DurationMs,
    string? FailureReason);
