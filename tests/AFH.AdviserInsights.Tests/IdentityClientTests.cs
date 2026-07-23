using System.Net;
using AFH.AdviserInsights.Infrastructure.Auth;
using AFH.AdviserInsights.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Tests;

public sealed class IdentityClientTests
{
    [Fact]
    public async Task GetCurrentUserAsync_WhenInternalTokenConfigured_CallsInternalIdentityEndpoint()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "success": true,
                  "data": {
                    "userId": "user-1",
                    "email": "adviser@afh.com",
                    "displayName": "Adviser One",
                    "adviserId": "adv-1",
                    "roles": [ "AUM Adviser" ],
                    "permissions": [ "Aum.Read.Self" ]
                  }
                }
                """)
        });
        var client = new IdentityClient(
            new HttpClient(handler),
            Options.Create(new AdviserInsightsOptions
            {
                Identity = new DownstreamIdentityOptions
                {
                    BaseUrl = "https://location.example/",
                    InternalToken = "internal-secret"
                }
            }),
            NullLogger<IdentityClient>.Instance);

        var user = await client.GetCurrentUserAsync("Bearer user-token", "correlation-1", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("https://location.example/api/internal/identity/v1/me", handler.Request!.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("internal-secret", handler.Request.Headers.Authorization.Parameter);
        Assert.True(handler.Request.Headers.TryGetValues("x-afh-user-token", out var userTokenValues));
        Assert.Equal("Bearer user-token", Assert.Single(userTokenValues));
    }

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(response);
        }
    }
}
