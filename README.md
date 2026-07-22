# AFH Adviser Insights Function App

Phase 1 read-only Adviser/AUM insights service backed by Snowflake.

## Purpose

This service proves the end-to-end slice:

```text
Copilot/MCP or API caller
 -> Adviser Insights API
 -> Identity/RBAC current user
 -> adviser or manager scope
 -> Snowflake adviser/client/policy/AUM queries
```

## Endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/health` | Health check. |
| `GET` | `/api/openapi/v1.json` | OpenAPI document. |
| `GET` | `/api/scalar` | Scalar API reference. |
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

The Phase 1 queries use these Snowflake tables:

```text
DIM_ADVISER
DIM_CUSTOMER
DIM_CUSTOMER_X_POLICY
DIM_XPLAN_POLICY
DIM_POLICY_SERVICE_START_END_DATE
FACT_AUM
```

Configure with:

```bash
AdviserInsights__Identity__BaseUrl=https://<identity-service>.azurewebsites.net
AdviserInsights__Snowflake__AccountUrl=https://<account>.snowflakecomputing.com
AdviserInsights__Snowflake__ApiToken=<token>
AdviserInsights__Snowflake__Warehouse=<warehouse>
AdviserInsights__Snowflake__Database=DIM_DB_DEV
AdviserInsights__Snowflake__Schema=AFH
AdviserInsights__Snowflake__Role=<role>
```

## Phase 1 Notes

The annual-review endpoint assumes `DIM_POLICY_SERVICE_START_END_DATE.POLICY_SERVICE_CLOSE_DATE` is the review/service date signal. Confirm this with the data team before production use.
