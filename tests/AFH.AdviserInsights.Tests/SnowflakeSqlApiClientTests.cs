using System.Net;
using System.Security.Cryptography;
using AFH.AdviserInsights.Infrastructure.Options;
using AFH.AdviserInsights.Infrastructure.Persistence.Snowflake;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Tests;

public sealed class SnowflakeSqlApiClientTests
{
    [Fact]
    public async Task QueryAsync_WithSnowflakeJwtConnectionString_SendsKeyPairJwtRequest()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "resultSetMetaData": {
                    "rowType": [
                      { "name": "CLIENT_ID" },
                      { "name": "AUM_VALUE" }
                    ]
                  },
                  "data": [
                    [ "123", "456.78" ]
                  ]
                }
                """)
        });
        var client = new SnowflakeSqlApiClient(
            new HttpClient(handler),
            Options.Create(new AdviserInsightsOptions
            {
                Snowflake = new SnowflakeOptions
                {
                    ConnectionString = $"account=bbv;host=hhh.west-europe.azure.snowflakecomputing.com;authenticator=snowflake_jwt;user=SOLDESIGN;private_key={privateKey}",
                    Warehouse = "WH",
                    Database = "DIM_DB_DEV",
                    Schema = "AFH",
                    Role = "PUBLIC"
                }
            }),
            TimeProvider.System);

        var rows = await client.QueryAsync("select 1", CancellationToken.None);

        Assert.Single(rows);
        Assert.Equal("https://hhh.west-europe.azure.snowflakecomputing.com/api/v2/statements", handler.Request!.RequestUri!.ToString());
        Assert.Contains(handler.Request.Headers.Accept, header => header.MediaType == "application/json");
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.False(string.IsNullOrWhiteSpace(handler.Request.Headers.Authorization.Parameter));
        Assert.True(handler.Request.Headers.TryGetValues("X-Snowflake-Authorization-Token-Type", out var values));
        Assert.Equal("KEYPAIR_JWT", Assert.Single(values));
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
