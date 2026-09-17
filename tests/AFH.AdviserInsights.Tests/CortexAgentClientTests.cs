using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AFH.AdviserInsights.Infrastructure.AI;
using AFH.AdviserInsights.Infrastructure.Options;
using AFH.AdviserInsights.Domain.Access;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Tests;

public sealed class CortexAgentClientTests
{
    [Fact]
    public async Task AskAsync_SendsJsonRequestWithCallerScope()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"message":"answer"}""", Encoding.UTF8, "application/json")
        });
        var options = Options.Create(new AdviserInsightsOptions
        {
            CortexAgent = new CortexAgentOptions
            {
                EndpointUrl = "https://snowflake.test/api/v2/databases/CORTEX_DB/schemas/RAW_DATA/agents/AFH_AGENT:run"
            }
        });
        var client = new CortexAgentClient(new HttpClient(handler), options, new StubAuthenticator());
        var scope = new AdviserDataScope("Team", "manager@afh.com", "manager-1", "Manager One", true, false);

        var result = await client.AskAsync("Show my team advisers", scope, "corr-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("AFH-Adviser-Insights/1.0", handler.Request!.Headers.UserAgent.ToString());
        Assert.Contains(handler.Request.Headers.Accept, header => header.MediaType == "application/json");
        Assert.Equal("corr-1", Assert.Single(handler.Request.Headers.GetValues("x-correlation-id")));
        Assert.Equal("applied", Assert.Single(handler.Request.Headers.GetValues("x-test-auth")));

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.False(payload.RootElement.GetProperty("stream").GetBoolean());
        var question = payload.RootElement
            .GetProperty("messages")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        Assert.Contains("team managed by 'Manager One'", question, StringComparison.Ordinal);
        Assert.Contains("Question: Show my team advisers", question, StringComparison.Ordinal);
    }

    [Fact]
    public void Authenticator_KeyPairJwt_CanGenerateConsecutiveTokensWithConfiguredContext()
    {
        using var rsa = RSA.Create(2048);
        var options = Options.Create(new AdviserInsightsOptions
        {
            CortexAgent = new CortexAgentOptions
            {
                AuthenticationMode = "KeyPairJwt",
                AccountIdentifier = "jr56660-ru01452",
                User = "soldesign",
                Role = "DEV_SOLDESIGN_ALL",
                Warehouse = "DEV_WH",
                PrivateKey = rsa.ExportPkcs8PrivateKeyPem()
            }
        });
        var authenticator = new CortexAgentAuthenticator(options);
        using var first = new HttpRequestMessage();
        using var second = new HttpRequestMessage();

        authenticator.Apply(first);
        authenticator.Apply(second);

        Assert.StartsWith("Bearer ey", first.Headers.Authorization?.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("Bearer ey", second.Headers.Authorization?.ToString(), StringComparison.Ordinal);
        Assert.Equal("KEYPAIR_JWT", Assert.Single(first.Headers.GetValues("X-Snowflake-Authorization-Token-Type")));
        Assert.Equal("DEV_SOLDESIGN_ALL", Assert.Single(first.Headers.GetValues("X-Snowflake-Role")));
        Assert.Equal("DEV_WH", Assert.Single(first.Headers.GetValues("X-Snowflake-Warehouse")));
    }

    [Fact]
    public async Task AskAsync_WhenSnowflakeReturnsEmptySuccess_TreatsResponseAsFailure()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
        });
        var options = Options.Create(new AdviserInsightsOptions
        {
            CortexAgent = new CortexAgentOptions
            {
                EndpointUrl = "https://snowflake.test/api/v2/databases/CORTEX_DB/schemas/RAW_DATA/agents/AFH_AGENT:run"
            }
        });
        var client = new CortexAgentClient(new HttpClient(handler), options, new StubAuthenticator());
        var scope = new AdviserDataScope("Self", "adviser@afh.com", "adviser-1", "Adviser One", false, false);

        var result = await client.AskAsync("Which advisers are in the survey?", scope, "corr-1", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(
            "Snowflake Cortex returned an empty response body.",
            result.Content.GetProperty("error").GetString());
        Assert.Equal("application/json; charset=utf-8", result.Content.GetProperty("contentType").GetString());
    }

    private sealed class StubAuthenticator : ICortexAgentAuthenticator
    {
        public void Apply(HttpRequestMessage request)
            => request.Headers.TryAddWithoutValidation("x-test-auth", "applied");
    }

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
    }
}
