# ADR-005: Rate-Limiting Middleware Choice

**Status**: Accepted
**Date**: 2026-05-12
**Feature**: security-review-2026-05
**Decider**: Morgan (Solution Architect) based on Story S-6 (DISCUSS wave)

---

## Context

Story S-6 calls for per-IP rate limiting on auth-adjacent endpoints (`/api/auth/login`, `/api/auth/callback`, `POST/DELETE /api/apikeys`, `POST /api/authorization/bootstrap/system-admin`) to bound brute-force / enumeration attacks against the auth surface.

Lighthouse is a self-hosted product. Deployments range from single-VM home installs to containerised reverse-proxy fronted instances. There is **no guaranteed Redis or other external state store** in the deployment topology — `appsettings.json` and the application database are the only persistence the product can rely on.

Three options were evaluated.

---

## Decision

**Use the built-in `Microsoft.AspNetCore.RateLimiting` middleware (`AddRateLimiter`, available in .NET 8 BCL) with a fixed-window policy partitioned by client IP.** Configuration is read from `appsettings.json` under a new `RateLimits` section.

The middleware is registered in `Program.cs` in this order: `UseForwardedHeaders → UseCors → UseRouting → UseRateLimiter → UseAuthentication → UseAuthorization → MapControllers`. That is, `UseRateLimiter` runs **before** `UseAuthentication` (so unauthenticated brute-force bursts are throttled before the auth handler does any work) and **after** `UseRouting` and `UseForwardedHeaders` (so endpoint metadata is available to scope policies to named routes and the partition key sees the resolved client IP).

The chosen policy:

- **Window**: 60 seconds, fixed (`FixedWindowRateLimiter`).
- **Permit limit**: configurable per policy in `appsettings.json`; default 100 for `/api/auth/login`, 20 for `/api/apikeys` POST/DELETE, 5 for `bootstrap/system-admin`.
- **Partition key**: forwarded client IP (after `UseForwardedHeaders`), with the connection remote IP as fallback.
- **Reject behaviour**: HTTP 429 with `Retry-After` header set to the remaining window.

Disable switch: `RateLimits:Enabled = false` short-circuits the middleware (useful in CI / E2E test environments that intentionally exceed limits).

---

## Alternatives Considered

### Option A: Built-in `Microsoft.AspNetCore.RateLimiting` (selected)

**Accepted because**:
- Ships with .NET 8; zero new NuGet dependency.
- In-memory state matches Lighthouse's self-hosted, single-process deployment topology. No external store required.
- Native endpoint-metadata integration: policies are attachable per route via `RequireRateLimiting("policy-name")` on `MapControllers` filters or via attributes on controller methods.
- MIT-licensed (Microsoft, .NET Foundation).
- Built-in `FixedWindowRateLimiter`, `SlidingWindowRateLimiter`, `TokenBucketRateLimiter`, and `ConcurrencyLimiter` cover any future variation without a library swap.

### Option B: `AspNetCoreRateLimit` (rejected for v1; future-only)

Third-party MIT NuGet package supporting IP / client-ID limits with optional Redis-backed distributed state.

**Rejected because**:
- Lighthouse is single-instance for the foreseeable future. The Redis advantage is irrelevant.
- Adds a dependency for behaviour the BCL now covers natively.
- The maintainer has flagged the package as being in maintenance mode now that the BCL has equivalent functionality.

If Lighthouse ever needs multi-instance horizontal scaling, this is the upgrade path — but that is a future-only consideration, not v1.

### Option C: Operator responsibility — document as residual risk only (rejected)

Recommend operators put a rate limiter on their reverse proxy (nginx `limit_req_zone`, Traefik plugin, etc.) and document this in the technical file.

**Rejected because**:
- CRA Annex I §1(2)(h) treats DoS resilience as a product capability, not a deployment configuration. The CRA self-assessment cannot honestly claim coverage of §1(2)(h) without an in-product mechanism.
- Many self-hosted operators run the app behind a simple TLS-terminating proxy with no rate-limit feature.
- Built-in option exists with negligible cost; not using it is a missed safety net for free.

---

## Consequences

**Positive**:
- Brute-force / enumeration attempts against auth endpoints get a hard ceiling regardless of deployment topology.
- CRA self-assessment can now cite an in-product mitigation for §1(2)(h).
- Configuration is operator-facing: limits can be tuned per deployment without code change.

**Negative**:
- In-memory state means each app-instance has its own limiter — for an attacker to be throttled they must hit the same instance. This is acceptable today (single-instance deployment). Documented as a residual risk in the CRA technical file: "rate limiter is per-instance; multi-instance horizontal scaling will require a distributed limiter."
- A trusted-IP-aware test fixture is needed for E2E so that the test suite isn't itself throttled. Mitigated by `RateLimits:Enabled = false` in test config.

**Quality attribute impact**:
- Security: improved (DoS resilience, brute-force bounding).
- Operability: minor surface area added — operators must understand and may tune the `RateLimits` config section.
- Performance: negligible — fixed-window counter is O(1) per request.
