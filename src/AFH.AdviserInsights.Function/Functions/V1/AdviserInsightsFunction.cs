using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Application.Abstractions.Audit;
using AFH.AdviserInsights.Application.Services;
using AFH.AdviserInsights.Contract;
using AFH.AdviserInsights.Function.Http;
using AFH.Common.Errors.AzureFunctions.Builders;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AFH.AdviserInsights.Function.Functions.V1;

public sealed class AdviserInsightsFunction(
    AdviserInsightsService insights,
    IAdviserInsightsAuditSink auditSink,
    AzureFunctionErrorResponseBuilder errorResponseBuilder)
{
    [Function("AdviserInsights_GetMyAdviser")]
    public async Task<HttpResponseData> GetMyAdviser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/adviser")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await ExecuteAsync(
            request,
            "GetMyAdviser",
            () => insights.GetMyAdviserAsync(Auth(request), Correlation(request), Target(request), cancellationToken),
            cancellationToken);

    [Function("AdviserInsights_GetMyTeamAdvisers")]
    public async Task<HttpResponseData> GetMyTeamAdvisers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/team/advisers")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await ExecuteAsync(
            request,
            "GetMyTeamAdvisers",
            () => insights.GetMyTeamAdvisersAsync(Auth(request), Correlation(request), Target(request), cancellationToken),
            cancellationToken);

    [Function("AdviserInsights_GetMyClients")]
    public async Task<HttpResponseData> GetMyClients(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/clients")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await ExecuteAsync(
            request,
            "GetMyClients",
            () => insights.GetMyClientsAsync(Auth(request), Correlation(request), Target(request), request.QueryInt("pageSize", 25), cancellationToken),
            cancellationToken);

    [Function("AdviserInsights_GetMyPolicies")]
    public async Task<HttpResponseData> GetMyPolicies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/policies")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await ExecuteAsync(
            request,
            "GetMyPolicies",
            () => insights.GetMyPoliciesAsync(Auth(request), Correlation(request), Target(request), request.QueryInt("pageSize", 25), cancellationToken),
            cancellationToken);

    [Function("AdviserInsights_GetMyAumSummary")]
    public async Task<HttpResponseData> GetMyAumSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/aum-summary")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await ExecuteAsync(
            request,
            "GetMyAumSummary",
            () => insights.GetMyAumSummaryAsync(Auth(request), Correlation(request), Target(request), cancellationToken),
            cancellationToken);

    [Function("AdviserInsights_GetMyHighValueClients")]
    public async Task<HttpResponseData> GetMyHighValueClients(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/clients/highest-policy-value")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await ExecuteAsync(
            request,
            "GetMyHighValueClients",
            () => insights.GetMyHighValueClientsAsync(Auth(request), Correlation(request), Target(request), request.QueryInt("pageSize", 25), cancellationToken),
            cancellationToken);

    [Function("AdviserInsights_GetMyClientsMissingAnnualReview")]
    public async Task<HttpResponseData> GetMyClientsMissingAnnualReview(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/clients/missing-annual-review")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await ExecuteAsync(
            request,
            "GetMyClientsMissingAnnualReview",
            () => insights.GetMyClientsMissingAnnualReviewAsync(Auth(request), Correlation(request), Target(request), request.QueryInt("pageSize", 25), cancellationToken),
            cancellationToken);

    [Function("AdviserInsights_AskCortexAgent")]
    public async Task<HttpResponseData> AskCortexAgent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/insights/ask")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await ExecuteAsync(
            request,
            "AskCortexAgent",
            async () =>
            {
                var body = await request.ReadFromJsonAsync<CortexQuestionRequest>(cancellationToken).ConfigureAwait(false);
                return await insights.AskCortexAgentAsync(
                        Auth(request),
                        Correlation(request),
                        body?.Question,
                        cancellationToken)
                    .ConfigureAwait(false);
            },
            cancellationToken);

    private async Task<HttpResponseData> ExecuteAsync<T>(
        HttpRequestData request,
        string operationName,
        Func<Task<ServiceResult<T>>> action,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action().ConfigureAwait(false);
            stopwatch.Stop();
            await WriteAuditAsync(
                    request,
                    operationName,
                    result.StatusCode,
                    result.IsSuccess ? null : FailureReason(result.ErrorMessage, result.ErrorDetail),
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken)
                .ConfigureAwait(false);

            return await request.ToJsonAsync(result, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await WriteAuditAsync(request, operationName, HttpStatusCode.InternalServerError, ex.Message, stopwatch.ElapsedMilliseconds, cancellationToken)
                .ConfigureAwait(false);

            return await errorResponseBuilder.BuildAsync(request, ex, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task WriteAuditAsync(
        HttpRequestData request,
        string operationName,
        HttpStatusCode statusCode,
        string? failureReason,
        long durationMs,
        CancellationToken cancellationToken)
    {
        var isSuccess = (int)statusCode is >= 200 and < 400;
        return auditSink.WriteAsync(
            new AdviserInsightsAuditEvent(
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow,
                operationName,
                request.Method,
                request.Url.AbsolutePath,
                string.IsNullOrWhiteSpace(request.Url.Query) ? null : request.Url.Query,
                isSuccess ? "Succeeded" : "Failed",
                (int)statusCode,
                Correlation(request),
                Actor(request),
                durationMs,
                isSuccess ? null : failureReason),
            cancellationToken);
    }

    private static string? Auth(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues("Authorization", out var authHeaders))
            return null;

        foreach (var authHeader in authHeaders)
        {
            var parts = authHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!part.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    continue;

                var token = part["Bearer ".Length..].Trim();
                if (!string.IsNullOrWhiteSpace(token))
                    return token;
            }
        }

        return null;
    }

    private static string? Correlation(HttpRequestData request) => request.Header("x-correlation-id");

    private static string? FailureReason(string? message, string? detail)
        => string.IsNullOrWhiteSpace(detail)
            ? message
            : $"{message} Detail: {detail}";

    private static string? Actor(HttpRequestData request)
        => request.Header("x-afh-ai-actor-id") ??
           request.Header("x-ms-client-principal-name") ??
           request.Header("x-ms-client-principal-id");

    private static AdviserTargetFilter? Target(HttpRequestData request)
    {
        var adviserId = request.Query("adviserId");
        var adviserName = request.Query("adviserName");
        var adviserEmail = request.Query("adviserEmail");

        return string.IsNullOrWhiteSpace(adviserId) &&
               string.IsNullOrWhiteSpace(adviserName) &&
               string.IsNullOrWhiteSpace(adviserEmail)
            ? null
            : new AdviserTargetFilter(adviserId, adviserName, adviserEmail);
    }
}
