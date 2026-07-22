using System.Net;
using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Contract;
using Microsoft.Azure.Functions.Worker.Http;

namespace AFH.AdviserInsights.Function.Http;

public static class HttpResponseExtensions
{
    public static async Task<HttpResponseData> ToJsonAsync<T>(
        this HttpRequestData request,
        ServiceResult<T> result,
        CancellationToken cancellationToken)
    {
        if (result.IsSuccess)
        {
            return await request.JsonAsync(HttpStatusCode.OK, ApiEnvelope<T>.Ok(result.Value!), cancellationToken)
                .ConfigureAwait(false);
        }

        return await request.JsonAsync(
                result.StatusCode,
                ApiEnvelope<T>.Fail(result.ErrorCode ?? "Error", result.ErrorMessage ?? "Request failed.", result.ErrorDetail),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<HttpResponseData> JsonAsync<T>(
        this HttpRequestData request,
        HttpStatusCode statusCode,
        T body,
        CancellationToken cancellationToken)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(body, cancellationToken).ConfigureAwait(false);
        return response;
    }
}
