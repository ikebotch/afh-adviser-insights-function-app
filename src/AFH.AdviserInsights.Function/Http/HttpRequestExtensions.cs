using Microsoft.Azure.Functions.Worker.Http;

namespace AFH.AdviserInsights.Function.Http;

public static class HttpRequestExtensions
{
    public static string? Header(this HttpRequestData request, string name)
        => request.Headers.TryGetValues(name, out var values)
            ? values.FirstOrDefault()
            : null;

    public static int QueryInt(this HttpRequestData request, string name, int fallback)
        => int.TryParse(System.Web.HttpUtility.ParseQueryString(request.Url.Query)[name], out var value)
            ? value
            : fallback;

    public static string? Query(this HttpRequestData request, string name)
    {
        var value = System.Web.HttpUtility.ParseQueryString(request.Url.Query)[name];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
