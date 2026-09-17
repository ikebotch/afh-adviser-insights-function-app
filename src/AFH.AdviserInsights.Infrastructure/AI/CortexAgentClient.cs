using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AFH.AdviserInsights.Application.Abstractions.AI;
using AFH.AdviserInsights.Domain.Access;
using AFH.AdviserInsights.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Infrastructure.AI;

public sealed class CortexAgentClient(
    HttpClient httpClient,
    IOptions<AdviserInsightsOptions> options,
    ICortexAgentAuthenticator authenticator) : ICortexAgentClient
{
    private const string UserAgent = "AFH-Adviser-Insights/1.0";

    public async Task<CortexAgentResult> AskAsync(
        string question,
        AdviserDataScope scope,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var endpoint = options.Value.CortexAgent.EndpointUrl;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
            throw new InvalidOperationException("AdviserInsights:CortexAgent:EndpointUrl must be an absolute URL.");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd(UserAgent);
        if (!string.IsNullOrWhiteSpace(correlationId))
            request.Headers.TryAddWithoutValidation("x-correlation-id", correlationId);

        authenticator.Apply(request);
        request.Content = JsonContent.Create(new
        {
            stream = false,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = BuildScopedQuestion(question, scope)
                        }
                    }
                }
            }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var content = ParseResponse(body);
        return new CortexAgentResult(response.IsSuccessStatusCode, (int)response.StatusCode, content);
    }

    private static string BuildScopedQuestion(string question, AdviserDataScope scope)
    {
        var boundary = scope.IncludeAll
            ? "The caller has organisation-wide AUM read access."
            : scope.IncludeTeam
                ? $"Only use data belonging to the adviser team managed by '{scope.ManagerName ?? scope.Email ?? "the signed-in manager"}'."
                : $"Only use data belonging to adviser id '{scope.AdviserId ?? "unknown"}' or adviser email '{scope.Email ?? "unknown"}'.";

        return $"AFH access boundary: {boundary} Do not return data outside this boundary.\n\nQuestion: {question}";
    }

    private static JsonElement ParseResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return JsonSerializer.SerializeToElement(new { });

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(new { rawResponse = body });
        }
    }
}
