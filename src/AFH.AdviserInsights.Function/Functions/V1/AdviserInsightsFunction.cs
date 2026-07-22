using AFH.AdviserInsights.Application.Services;
using AFH.AdviserInsights.Function.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AFH.AdviserInsights.Function.Functions.V1;

public sealed class AdviserInsightsFunction(AdviserInsightsService insights)
{
    [Function("AdviserInsights_GetMyAdviser")]
    public async Task<HttpResponseData> GetMyAdviser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/adviser")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await request.ToJsonAsync(
            await insights.GetMyAdviserAsync(Auth(request), Correlation(request), cancellationToken).ConfigureAwait(false),
            cancellationToken);

    [Function("AdviserInsights_GetMyTeamAdvisers")]
    public async Task<HttpResponseData> GetMyTeamAdvisers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/team/advisers")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await request.ToJsonAsync(
            await insights.GetMyTeamAdvisersAsync(Auth(request), Correlation(request), cancellationToken).ConfigureAwait(false),
            cancellationToken);

    [Function("AdviserInsights_GetMyClients")]
    public async Task<HttpResponseData> GetMyClients(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/clients")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await request.ToJsonAsync(
            await insights.GetMyClientsAsync(Auth(request), Correlation(request), request.QueryInt("pageSize", 25), cancellationToken).ConfigureAwait(false),
            cancellationToken);

    [Function("AdviserInsights_GetMyPolicies")]
    public async Task<HttpResponseData> GetMyPolicies(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/policies")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await request.ToJsonAsync(
            await insights.GetMyPoliciesAsync(Auth(request), Correlation(request), request.QueryInt("pageSize", 25), cancellationToken).ConfigureAwait(false),
            cancellationToken);

    [Function("AdviserInsights_GetMyAumSummary")]
    public async Task<HttpResponseData> GetMyAumSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/aum-summary")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await request.ToJsonAsync(
            await insights.GetMyAumSummaryAsync(Auth(request), Correlation(request), cancellationToken).ConfigureAwait(false),
            cancellationToken);

    [Function("AdviserInsights_GetMyHighValueClients")]
    public async Task<HttpResponseData> GetMyHighValueClients(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/clients/highest-policy-value")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await request.ToJsonAsync(
            await insights.GetMyHighValueClientsAsync(Auth(request), Correlation(request), request.QueryInt("pageSize", 25), cancellationToken).ConfigureAwait(false),
            cancellationToken);

    [Function("AdviserInsights_GetMyClientsMissingAnnualReview")]
    public async Task<HttpResponseData> GetMyClientsMissingAnnualReview(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/clients/missing-annual-review")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await request.ToJsonAsync(
            await insights.GetMyClientsMissingAnnualReviewAsync(Auth(request), Correlation(request), request.QueryInt("pageSize", 25), cancellationToken).ConfigureAwait(false),
            cancellationToken);

    private static string? Auth(HttpRequestData request) => request.Header("Authorization");

    private static string? Correlation(HttpRequestData request) => request.Header("x-correlation-id");
}
