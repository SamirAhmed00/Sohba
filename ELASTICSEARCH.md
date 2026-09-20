# Sohba — Elasticsearch Log Storage (logs only)

> **SCOPE LOCK** — Elasticsearch in Sohba is **log storage, log search and log
> analysis only**. Application/business search (users, posts, groups, pages,
> hashtags, business data) remains **100% SQL-based** via `SearchService` and
> EF Core. Do not index business data in Elasticsearch.

---

## 1. Architecture

| Aspect | Choice |
|---|---|
| Serilog sink | **`Elastic.Serilog.Sinks` 9.0.0** (official Elastic; replaces the deprecated `Serilog.Sinks.Elasticsearch`) |
| Document format | **Elastic Common Schema (ECS)** — built into the sink (`Elastic.CommonSchema.Serilog`) |
| Storage target | **Data stream** `logs-sohba-<environment>` (e.g. `logs-sohba-development`) — the sink's officially recommended 8.x model; no write aliases or manual index management |
| Bootstrapping | `BootstrapMethod.Silent` — the sink installs the ECS component/index templates for the data stream on first use; failures never break the app |
| Batching | Built-in bounded in-memory channel (`Elastic.Ingest.Elasticsearch`): batches up to 500 events, flush at 5 s max lifetime, 2 concurrent exporters, retries with backoff. **No `Serilog.Sinks.Async` needed** — `Emit()` is a non-blocking `TryWrite` |
| Failure isolation | Under an Elasticsearch outage events are **dropped** (`BoundedChannelFullMode.DropWrite`, 10 000-event inbound buffer) instead of blocking requests. The Console + File sinks keep logging and remain the durable fallback |
| Existing sinks | Console and File sinks unchanged (path `logs/sohba-.log`, daily rolling, 30 retained) |

### Field mapping (ECS)

| Serilog property | ECS document location (verified live) |
|---|---|
| `CorrelationId` | `labels.CorrelationId` |
| `UserId` | `user.id` (auto-mapped to the ECS user object by the sink) |
| `Application` | `labels.Application` |
| `Environment` | `labels.Environment` |
| `Method` (HTTP request logs) | `http.request.method` |
| `Path` | `url.path` |
| `StatusCode` | `http.response.status_code` |
| `ElapsedMs` | `metadata.ElapsedMs` (milliseconds) |
| Exceptions | `error.type`, `error.message`, `error.stack_trace` |
| Numeric entity IDs (`PostId`, `StoryId`, `ReportId`, …) | `metadata.*` — preserved as-is, never renamed |
| String properties without an ECS mapping | `labels.*` |

## 2. Configuration (secrets)

The sink activates **only** when `SOHBA_ES_URL` is configured — the app runs
normally without Elasticsearch. Values are read from the standard ASP.NET Core
configuration (environment variables / user-secrets / `.env`), **never** from
`appsettings.json` or source code.

| Variable | Meaning | Example |
|---|---|---|
| `SOHBA_ES_URL` | Elasticsearch endpoint (absent ⇒ sink disabled) | `http://localhost:9200` |
| `SOHBA_ES_USER` | Basic-auth user (optional) | `elastic` |
| `SOHBA_ES_PASSWORD` | Basic-auth password (optional) | — |
| `SOHBA_ES_ILM_POLICY` | Optional ILM policy name for retention (see §4) | `sohba-logs-dev` |

Local dev options:

```bash
# Option A: copy the template and set values
cp .env.example .env

# Option B: dotnet user-secrets (Development environment only)
dotnet user-secrets set "SOHBA_ES_URL"     "http://localhost:9200"
dotnet user-secrets set "SOHBA_ES_USER"    "elastic"
dotnet user-secrets set "SOHBA_ES_PASSWORD" "<your-dev-password>"
```

`.env` files are git-ignored (`.gitignore`); `.env.example` is the tracked
template with placeholders only.

## 3. Running Elasticsearch locally (development only)

```bash
cp .env.example .env            # set SOHBA_ES_PASSWORD
docker compose -f docker-compose.dev.yml up -d elasticsearch

# optional Kibana (operations tooling; Sohba does NOT require it)
docker compose -f docker-compose.dev.yml --profile kibana up -d
# Kibana: http://127.0.0.1:5601  (user: elastic, password: SOHBA_ES_PASSWORD)
```

- Single node, Elasticsearch 8.19 (sink 9.0.0 requires ≥ 8.15).
- Security enabled; ports bound to `127.0.0.1` only; TLS disabled for
  plain-HTTP localhost development. Production must use TLS + real secrets.

Then start Sohba with the `SOHBA_ES_*` variables set and make a few requests.
Verify the data stream exists:

```bash
curl -u elastic:<password> "http://localhost:9200/_data_stream/logs-sohba-development"
```

## 4. Retention (native ILM — 7 days dev / 30 days prod)

The sink applies an ILM policy by name through `SOHBA_ES_ILM_POLICY`. Create
the policy once in Elasticsearch (Kibana Dev Tools or curl):

```jsonc
// Development: delete after 7 days
PUT _ilm/policy/sohba-logs-dev
{
  "policy": {
    "phases": {
      "hot": {
        "actions": {
          "rollover": { "max_primary_shard_size": "50gb", "max_age": "1d" }
        }
      },
      "delete": { "min_age": "7d", "actions": { "delete": {} } }
    }
  }
}

// Production: delete after 30 days
PUT _ilm/policy/sohba-logs-prod
{
  "policy": {
    "phases": {
      "hot": {
        "actions": {
          "rollover": { "max_primary_shard_size": "50gb", "max_age": "1d" }
        }
      },
      "delete": { "min_age": "30d", "actions": { "delete": {} } }
    }
  }
}
```

Then set `SOHBA_ES_ILM_POLICY=sohba-logs-dev` (or `sohba-logs-prod`) **before**
starting Sohba. When unset, Elasticsearch's shipped default `logs` policy
applies (rollover at 50 GB / 30 days, no delete phase).

## 5. Verifying logs (Kibana → Discover → data stream `logs-sohba-*`)

| Goal | Kibana query (KQL) |
|---|---|
| By CorrelationId | `labels.CorrelationId: "<id>"` |
| By UserId | `user.id: "<user-guid>"` |
| By endpoint/path | `url.path: "/Posts/Details"` or `url.path: /Posts*` |
| 5xx requests | `http.response.status_code >= 500` |
| Slow requests | `metadata.ElapsedMs > 1000` |
| Exceptions | `error.type: *` or `error.stack_trace: *` |
| One app/env only | `labels.Application: "Sohba" and labels.Environment: "Development"` |

Equivalently with the Elasticsearch API:

```jsonc
GET logs-sohba-*/_search
{
  "query": {
    "bool": {
      "must": [ { "match": { "labels.CorrelationId": "<id>" } } ]
      // 5xx:  { "range": { "http.response.status_code": { "gte": 500 } } }
      // slow: { "range": { "metadata.ElapsedMs": { "gt": 1000 } } }
      // exceptions: { "exists": { "field": "error.stack_trace" } }
    }
  }
}
```

## 6. Failure drill (expected behavior)

1. Start Elasticsearch, start Sohba, generate requests → logs appear in
   `logs-sohba-development` (first startup may take a few seconds for
   template bootstrapping).
2. `docker compose -f docker-compose.dev.yml stop elasticsearch`.
3. Keep making requests → **all requests keep succeeding**; logging to the
   Console/File sinks continues; ES-bound events are buffered then dropped —
   never an app failure or added request latency.
4. `docker compose -f docker-compose.dev.yml start elasticsearch` →
   shipping resumes automatically for buffered events; dropped events are
   intentionally lost (File sink remains the durable record).

## 7. Production setup (manual, do later)

- Real Elasticsearch cluster (8.15+) with TLS; put `SOHBA_ES_*` values in the
  platform secret store (Azure Key Vault / AWS Secrets Manager / k8s secrets),
  exposed as environment variables.
- Set `SOHBA_ES_ILM_POLICY=sohba-logs-prod` (§4) for 30-day retention.
- Optionally use API-key auth instead of basic auth (create an API key with
  only the logging role) — the sink supports keys via the same package.
- Do **not** deploy business data or application search to Elasticsearch.
