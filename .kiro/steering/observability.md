---
inclusion: always
---

# Observability

Observability is a core, non-negotiable requirement for every feature. All three pillars — structured logs, distributed traces, and metrics — MUST be implemented using **OpenTelemetry (OTel)** and wired through **.NET Aspire** for local development and cloud-independent deployment.

---

## Stack

| Concern | Tool |
|---|---|
| Instrumentation | OpenTelemetry SDK (`OpenTelemetry.Extensions.Hosting`) |
| Orchestration / dev dashboard | .NET Aspire (`Aspire.Hosting`, `Aspire.Dashboard`) |
| Trace / metric export | OTel OTLP exporter (cloud-agnostic) |
| Log export | OTel OTLP log exporter |
| Local sink | Aspire Dashboard (traces, metrics, logs in one UI) |
| Cloud sink | Any OTLP-compatible backend (Azure Monitor, Grafana, Datadog, etc.) — configured via environment variables, not hardcoded |

---

## Pillar 1 — Structured Logging

- Use `Microsoft.Extensions.Logging` with OTel log bridge — never `Console.WriteLine` or `Debug.WriteLine`.
- All log messages MUST be structured (key-value pairs), not interpolated strings.
- Every log entry MUST include: `TraceId`, `SpanId`, `ServiceName`, `Environment`.
- Log levels MUST be used correctly:
  - `Debug` — internal diagnostic detail (disabled in production by default)
  - `Information` — normal operational events
  - `Warning` — recoverable unexpected conditions
  - `Error` — failures that need attention
  - `Critical` — system-level failures requiring immediate action
- NEVER log sensitive data: passwords, tokens, connection strings, PII, or raw spatial coordinates.
- Use `LoggerMessage.Define` (source-generated logging) for hot-path log calls to avoid allocation overhead.

---

## Pillar 2 — Distributed Tracing

- Every inbound HTTP request MUST produce a trace span via the ASP.NET Core OTel instrumentation.
- Every outbound HTTP call MUST propagate trace context via `HttpClient` OTel instrumentation.
- Every EF Core database query MUST be traced via `OpenTelemetry.Instrumentation.EntityFrameworkCore`.
- Custom business operations that span multiple steps MUST create child spans using `ActivitySource`.
- Span names MUST follow the format `{Verb} {Resource}` (e.g. `GET /users/{id}`, `Query Users`).
- Spans MUST include relevant attributes: HTTP method, status code, DB statement (sanitised), error type.
- Trace context MUST be propagated across service boundaries using W3C TraceContext headers.

---

## Pillar 3 — Metrics

- Expose the following default metrics via OTel:
  - HTTP request duration (histogram, by route and status code)
  - HTTP request count (counter, by route and status code)
  - Active HTTP connections (gauge)
  - EF Core query duration (histogram)
  - GC and runtime metrics via `OpenTelemetry.Instrumentation.Runtime`
- Custom business metrics MUST use `System.Diagnostics.Metrics` (`Meter`, `Counter<T>`, `Histogram<T>`).
- Metric names MUST follow OTel semantic conventions: `http.server.request.duration`, `db.client.operation.duration`, etc.
- All metrics MUST include `service.name`, `service.version`, and `deployment.environment` resource attributes.

---

## .NET Aspire Integration

- The solution MUST include an Aspire AppHost project (`src/AppHost`) that orchestrates the API, frontend, and database for local development.
- The Aspire Dashboard MUST be the default local observability UI — no separate local tooling required.
- Service defaults (OTel setup, health checks, resilience) MUST be applied via the Aspire `ServiceDefaults` project (`src/ServiceDefaults`).
- All OTel exporters MUST be configured via environment variables / Aspire resource bindings — never hardcoded endpoints or keys.

```csharp
// ServiceDefaults/Extensions.cs — applied to every service
builder.AddServiceDefaults(); // registers OTel, health checks, resilience
```

---

## Alerting Standards

Alerting is configured at the observability backend level (cloud-agnostic). The following thresholds MUST be defined as part of every feature's deployment configuration:

| Signal | Default Alert Condition |
|---|---|
| HTTP 5xx error rate | > 1% of requests over 5 minutes |
| HTTP p99 latency | > 2 seconds over 5 minutes |
| HTTP p95 latency | > 1 second over 5 minutes |
| Unhandled exception rate | Any increase above baseline |
| DB query p99 duration | > 500 ms over 5 minutes |
| Service health check | Failing for > 1 minute |

- Alert thresholds MUST be documented in the feature's ADR or deployment notes.
- Alerts MUST route to a named owner — no ownerless alerts.
- Every alert MUST have a runbook link or inline remediation notes.

---

## Health Checks

- Every service MUST expose `/health/live` (liveness) and `/health/ready` (readiness) endpoints.
- Health checks MUST cover: database connectivity, any external service dependencies.
- Health checks are registered via Aspire `ServiceDefaults` and MUST not be removed.

---

## Observability Rules for Generated Code

- NEVER generate code that swallows exceptions silently — always log at `Error` or rethrow.
- NEVER use `Console.WriteLine` for diagnostic output — use `ILogger<T>`.
- EVERY new service class MUST accept `ILogger<T>` via constructor injection.
- EVERY new `ActivitySource` MUST be registered with the OTel tracer provider.
- EVERY new `Meter` MUST be registered with the OTel meter provider.
- When adding a new API endpoint, ensure the OTel ASP.NET Core instrumentation covers it (it does by default — do not opt out).
