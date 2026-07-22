namespace AFH.AdviserInsights.Domain.Access;

public sealed record AdviserDataScope(
    string AccessMode,
    string? Email,
    string? AdviserId,
    string? ManagerName,
    bool IncludeTeam,
    bool IncludeAll);
