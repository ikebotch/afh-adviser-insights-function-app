using AFH.AdviserInsights.Function.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace AFH.AdviserInsights.Function.Functions.V1;

public sealed class HealthFunction
{
    [Function("AdviserInsights_Health")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await request.JsonAsync(
            HttpStatusCode.OK,
            new
            {
                service = "AFH.AdviserInsights",
                status = "Healthy",
                utcNow = DateTimeOffset.UtcNow
            },
            cancellationToken);
}
