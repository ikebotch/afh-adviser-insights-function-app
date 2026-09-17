# AFH Adviser Insights Function App

Phase 1 read-only Adviser/AUM insights service backed by Snowflake.

## Purpose

This service proves the end-to-end slice:

```text
Copilot/MCP or API caller
 -> Adviser Insights API
 -> Identity/RBAC current user
 -> adviser or manager scope
 -> Snowflake EF Core adviser/client/policy/AUM queries
```

## Endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/health` | Health check. |
| `GET` | `/api/openapi/v1.json` | OpenAPI document. |
| `GET` | `/api/scalar` | Scalar API reference. |
| `POST` | `/api/v1/insights/ask` | Ask the configured Snowflake Cortex agent within the signed-in user's AUM scope. |
| `GET` | `/api/v1/me/adviser` | Signed-in adviser profile. |
| `GET` | `/api/v1/me/team/advisers` | Managed advisers for manager users. |
| `GET` | `/api/v1/me/clients` | Clients in the signed-in user's adviser scope. |
| `GET` | `/api/v1/me/policies` | Policies in the signed-in user's adviser scope. |
| `GET` | `/api/v1/me/aum-summary` | AUM summary for self/team/all scope. |
| `GET` | `/api/v1/me/clients/highest-policy-value` | Highest-value clients. |
| `GET` | `/api/v1/me/clients/missing-annual-review` | Clients without annual policy review in the last 12 months. |

## Identity And RBAC

The service follows the Booking pattern by forwarding the caller bearer token to the Identity service `/api/v1/me` endpoint.

Phase 1 permissions:

```text
Aum.Read.Self
Aum.Read.Team
Aum.Read.All
```

Access behavior:

```text
Aum.Read.Self -> signed-in adviser only
Aum.Read.Team -> advisers managed by the signed-in manager
Aum.Read.All  -> unrestricted AUM scope for finance/admin users
```

## Snowflake

This service uses the same Snowflake EF Core approach as the Lead/Client Integration service. The persistence adapter lives under `AFH.AdviserInsights.Infrastructure/Persistence/Snowflake`, maps the dimensional tables as keyless read-only EF entities, and implements the application repository contract with LINQ queries over `AdviserInsightsSnowflakeDbContext`.

The Phase 1 queries use these Snowflake tables:

```text
DIM_ADVISER
DIM_CUSTOMER
DIM_CUSTOMER_X_POLICY
DIM_HOUSEHOLD_X_CUST
DIM_XPLAN_POLICY
DIM_POLICY_SERVICE_START_END_DATE
FACT_AUM
FACT_CUSTOMER
```

Configure with:

```bash
AdviserInsights__Identity__BaseUrl=https://<identity-service>.azurewebsites.net
AdviserInsights__Identity__InternalToken=<shared-internal-token>
AdviserInsights__Identity__CurrentUserPath=api/internal/identity/v1/me
AdviserInsights__Audit__Provider=TableStorage
AdviserInsights__Audit__ConnectionString=<storage-connection-string>
AdviserInsights__Audit__TableName=AdviserInsightsAudit
AdviserInsights__Snowflake__ConnectionString=account=<account>;host=<account>.<region>.azure.snowflakecomputing.com;authenticator=snowflake_jwt;user=<user>;private_key=<private-key-pem-or-base64>;warehouse=<warehouse>;role=<role>
AdviserInsights__Snowflake__Database=DIM_DB_DEV
AdviserInsights__Snowflake__Schema=AFH
AdviserInsights__CortexAgent__EndpointUrl=https://JR56660-RU01452.snowflakecomputing.com/api/v2/databases/CORTEX_DB/schemas/RAW_DATA/agents/AGENT_SURVEY_MONKEY_NBE:run
AdviserInsights__CortexAgent__AuthenticationMode=KeyPairJwt
AdviserInsights__CortexAgent__AccountIdentifier=JR56660-RU01452
AdviserInsights__CortexAgent__User=SOLDESIGN
AdviserInsights__CortexAgent__Role=DEV_SOLDESIGN_ALL
AdviserInsights__CortexAgent__Warehouse=DEV_WH
AdviserInsights__CortexAgent__PrivateKey=<snowflake-private-key-pem>
AdviserInsights__CortexAgent__PrivateKeyPassphrase=<optional-private-key-passphrase>
AdviserInsights__CortexAgent__JwtLifetimeMinutes=55
```

For key-pair authentication, pass the private key in the Snowflake EF connection string. In Azure, prefer Key Vault references for the connection string or private key value. The service appends `db` and `schema` from configuration when the connection string does not already include them, so Snowflake sessions have a current database/schema before EF queries run.

The Cortex agent uses its own REST configuration but can use the same Snowflake user and private key. Adviser Insights resolves the caller through Identity before invoking Cortex and adds the caller's self, team, or all-data boundary to the request. Snowflake row-access policies remain the authoritative data boundary.

## Audit

Direct Adviser Insights API calls are written to the configured audit sink. With `AdviserInsights__Audit__Provider=TableStorage`, rows are written to the `AdviserInsightsAudit` table using partition keys like `20260729|AdviserInsights`. These rows include operation name, route, query string, status code, duration, correlation id, actor header when available, and failure reason.

MCP tool calls are still audited by the MCP Gateway in `AiToolAudit`. Direct calls to this service will not appear under the `booking` partition in that table.

Unexpected exceptions are returned through the shared `AFH.Common.Errors.AzureFunctions` error response builder so 500 responses follow the common AFH error shape.

## Phase 1 Notes

The annual-review endpoint assumes `DIM_POLICY_SERVICE_START_END_DATE.POLICY_SERVICE_CLOSE_DATE` is the review/service date signal. Confirm this with the data team before production use.
