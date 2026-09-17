using AFH.AdviserInsights.Function.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AFH.AdviserInsights.Function.Functions.V1;

public sealed class DocsFunction
{
    [Function("AdviserInsights_OpenApiV1")]
    public async Task<HttpResponseData> OpenApi(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi/v1.json")] HttpRequestData request,
        CancellationToken cancellationToken)
        => await request.JsonAsync(HttpStatusCode.OK, CreateDocument(), cancellationToken);

    [Function("AdviserInsights_Scalar")]
    public async Task<HttpResponseData> Scalar(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scalar")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");
        var html = """
            <!doctype html>
            <html>
              <head>
                <title>AFH Adviser Insights API</title>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
              </head>
              <body>
                <script
                  id="api-reference"
                  data-url="/api/openapi/v1.json"
                  data-theme="purple">
                </script>
                <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
              </body>
            </html>
            """;
        await response.Body.WriteAsync(Encoding.UTF8.GetBytes(html), cancellationToken);
        return response;
    }

    private static object CreateDocument()
    {
        var paths = new Dictionary<string, object>
        {
            ["/api/health"] = Get("health", "Health", "Health check."),
            ["/api/v1/me/adviser"] = Get("getMyAdviser", "Adviser", "Get the signed-in adviser profile, or a target adviser profile in the signed-in user's scope.", hasAdviserFilter: true),
            ["/api/v1/me/team/advisers"] = Get("getMyTeamAdvisers", "Adviser", "Get advisers managed by the signed-in manager.", hasAdviserFilter: true),
            ["/api/v1/me/clients"] = Get("getMyClients", "Clients", "Get clients in the signed-in user's adviser scope.", hasPageSize: true, hasAdviserFilter: true),
            ["/api/v1/me/policies"] = Get("getMyPolicies", "Policies", "Get policies in the signed-in user's adviser scope.", hasPageSize: true, hasAdviserFilter: true),
            ["/api/v1/me/aum-summary"] = Get("getMyAumSummary", "AUM", "Get AUM summary in the signed-in user's adviser scope.", hasAdviserFilter: true),
            ["/api/v1/me/clients/highest-policy-value"] = Get("getMyHighValueClients", "Clients", "Find clients with highest policy/AUM value.", hasPageSize: true, hasAdviserFilter: true),
            ["/api/v1/me/clients/missing-annual-review"] = Get("getMyClientsMissingAnnualReview", "Clients", "Find clients without an annual policy review in the last 12 months.", hasPageSize: true, hasAdviserFilter: true),
            ["/api/v1/insights/ask"] = PostQuestion()
        };

        return new
        {
            openapi = "3.0.3",
            info = new
            {
                title = "AFH Adviser Insights API",
                version = "1.0.0",
                description = "Read-only Phase 1 Adviser/AUM insights over Snowflake."
            },
            paths,
            components = new
            {
                securitySchemes = new
                {
                    bearerAuth = new
                    {
                        type = "http",
                        scheme = "bearer",
                        bearerFormat = "JWT"
                    }
                }
            }
        };
    }

    private static object Get(string operationId, string tag, string summary, bool hasPageSize = false, bool hasAdviserFilter = false)
    {
        var operation = new Dictionary<string, object?>
        {
            ["tags"] = new[] { tag },
            ["operationId"] = operationId,
            ["summary"] = summary,
            ["security"] = new object[] { new Dictionary<string, string[]> { ["bearerAuth"] = [] } },
            ["responses"] = new Dictionary<string, object>
            {
                ["200"] = new { description = "Successful response." },
                ["401"] = new { description = "Unauthorized." },
                ["403"] = new { description = "Forbidden." }
            }
        };

        var parameters = new List<object>();

        if (hasPageSize)
        {
            parameters.Add(new
            {
                name = "pageSize",
                @in = "query",
                required = false,
                schema = new { type = "integer", minimum = 1, maximum = 100 },
                description = "Maximum number of rows to return."
            });
        }

        if (hasAdviserFilter)
        {
            parameters.Add(QueryString("adviserName", "Optional adviser name filter, for example Aaron or Daniel."));
            parameters.Add(QueryString("adviserEmail", "Optional adviser email filter."));
            parameters.Add(QueryString("adviserId", "Optional numeric adviser identifier filter."));
        }

        if (parameters.Count > 0)
            operation["parameters"] = parameters;

        return new Dictionary<string, object?> { ["get"] = operation };
    }

    private static object QueryString(string name, string description)
        => new
        {
            name,
            @in = "query",
            required = false,
            schema = new { type = "string" },
            description
        };

    private static object PostQuestion()
        => new Dictionary<string, object?>
        {
            ["post"] = new
            {
                tags = new[] { "Cortex" },
                operationId = "askCortexAgent",
                summary = "Ask the configured Snowflake Cortex agent a question within the signed-in user's AUM scope.",
                security = new object[] { new Dictionary<string, string[]> { ["bearerAuth"] = [] } },
                requestBody = new
                {
                    required = true,
                    content = new Dictionary<string, object>
                    {
                        ["application/json"] = new
                        {
                            schema = new
                            {
                                type = "object",
                                required = new[] { "question" },
                                properties = new { question = new { type = "string" } }
                            }
                        }
                    }
                },
                responses = new Dictionary<string, object>
                {
                    ["200"] = new { description = "Successful Cortex response." },
                    ["400"] = new { description = "Question is missing." },
                    ["401"] = new { description = "Unauthorized." },
                    ["403"] = new { description = "Forbidden." },
                    ["502"] = new { description = "Cortex agent request failed." }
                }
            }
        };
}
