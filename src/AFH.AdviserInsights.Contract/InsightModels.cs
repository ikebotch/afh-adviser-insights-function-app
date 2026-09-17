namespace AFH.AdviserInsights.Contract;

using System.Text.Json;

public sealed record ApiEnvelope<T>(bool Success, T? Data, ApiProblem? Error = null)
{
    public static ApiEnvelope<T> Ok(T data) => new(true, data);

    public static ApiEnvelope<T> Fail(string code, string message, string? detail = null)
        => new(false, default, new ApiProblem(code, message, detail));
}

public sealed record ApiProblem(string Code, string Message, string? Detail = null);

public sealed record AdviserProfileResponse(
    string AdviserId,
    string AdviserName,
    string? EmailAddress,
    string? Manager,
    string? Region,
    string? Status,
    string AccessMode);

public sealed record TeamAdviserResponse(
    string AdviserId,
    string AdviserName,
    string? EmailAddress,
    string? Manager,
    string? Region,
    string? Status);

public sealed record ClientSummaryResponse(
    string ClientId,
    string? ClientName,
    string? AdviserId,
    string? AdviserName,
    string? Household,
    decimal? AumValue);

public sealed record PolicySummaryResponse(
    string PolicyId,
    string? ClientId,
    string? ClientName,
    string? AdviserId,
    string? AdviserName,
    string? ReportName,
    DateOnly? PolicyStartDate,
    DateOnly? PolicyServiceCloseDate,
    decimal? AumValue);

public sealed record AumSummaryResponse(
    string Scope,
    int AdviserCount,
    int ClientCount,
    int PolicyCount,
    decimal TotalAum);

public sealed record HighValueClientResponse(
    string ClientId,
    string? ClientName,
    string? AdviserId,
    string? AdviserName,
    decimal TotalPolicyValue,
    int PolicyCount);

public sealed record MissingAnnualReviewClientResponse(
    string ClientId,
    string? ClientName,
    string? AdviserId,
    string? AdviserName,
    DateOnly? LastPolicyServiceDate,
    int ActivePolicyCount);

public sealed record CortexQuestionRequest(string Question);

public sealed record CortexAnswerResponse(JsonElement Response, string AccessMode);
