namespace AFH.AdviserInsights.Application.Abstractions;

public sealed record CurrentUserContext(
    string? UserProfileId,
    string? ExternalSubject,
    string? Email,
    string? DisplayName,
    string? AdviserId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<UserAccessScope> AccessScopes);

public sealed record UserAccessScope(
    string Area,
    string ScopeType,
    string? ScopeValue,
    string? DisplayName);

public sealed record AdviserDataScope(
    string AccessMode,
    string? Email,
    string? AdviserId,
    string? ManagerName,
    bool IncludeTeam,
    bool IncludeAll);
