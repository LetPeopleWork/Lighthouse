# ADR-007: OAuth Provider Registry — String Key, DI-Resolved

**Status**: Accepted
**Date**: 2026-05-14
**Feature**: work-tracking-oauth-authentication
**Decider**: Morgan (Solution Architect) based on DISCUSS D3 (provider-name-as-string keying)

---

## Context

Epic 2438 introduces OAuth as a new authentication method for work-tracking-system connectors, starting with Jira (Atlassian 3LO) and Azure DevOps (Entra ID / Azure DevOps OAuth). Story 4971 requires the abstraction to be honest end-to-end — a third provider (Linear, GitHub, etc.) must be addable without touching `OAuthController`, `OAuthService`, or `OAuthCredential` persistence.

The existing `AuthenticationMethodSchema` (`Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodSchema.cs:32-150`) already keys authentication methods by string (`jira.cloud`, `jira.datacenter`, `jira.scopedtoken`, `ado.pat`, `linear.apikey`, `none`) and `WorkTrackingSystemConnection.AuthenticationMethodKey` (`Models/WorkTrackingSystemConnection.cs:15`) is a `string` column. Provider keying via this column is essentially free.

Three resolution strategies were evaluated.

---

## Decision

**Define a new outbound port `IOAuthProvider` (`Services/Interfaces/OAuth/IOAuthProvider.cs`) with a `ProviderKey` property and an `IOAuthProviderRegistry` (`Services/Interfaces/OAuth/IOAuthProviderRegistry.cs`) that resolves the implementation by string key from a DI-registered collection.**

```csharp
public interface IOAuthProvider
{
    string ProviderKey { get; }                                 // "jira.oauth", "ado.oauth", ...
    IReadOnlyList<string> DefaultScopes { get; }
    Uri BuildAuthorizationUrl(OAuthFlowContext context);
    Task<OAuthTokens> ExchangeCodeAsync(string code, OAuthFlowContext context, CancellationToken ct);
    Task<OAuthTokens> RefreshTokenAsync(string refreshToken, CancellationToken ct);
}

public interface IOAuthProviderRegistry
{
    IOAuthProvider GetByKey(string authenticationMethodKey);    // throws OAuthProviderNotFoundException on miss
}
```

`OAuthProviderRegistry` (concrete) takes `IEnumerable<IOAuthProvider>` in its constructor and builds a `Dictionary<string, IOAuthProvider>` once. DI registration in `Program.cs`:

```csharp
builder.Services.AddSingleton<IOAuthProvider, JiraOAuthProvider>();
builder.Services.AddSingleton<IOAuthProvider, AdoOAuthProvider>();
builder.Services.AddSingleton<IOAuthProviderRegistry, OAuthProviderRegistry>();
```

The new `AuthenticationMethodKeys` constants (`Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodKeys.cs`) — `JiraOAuth = "jira.oauth"`, `AzureDevOpsOAuth = "ado.oauth"` — match the `ProviderKey` strings exactly. Adding a third provider is three steps: (1) implement `IOAuthProvider`, (2) add a constant to `AuthenticationMethodKeys` and a row to `AuthenticationMethodSchema`, (3) register in `Program.cs`. Zero changes to `OAuthController`, `OAuthService`, `OAuthCredential`, or `OAuthProviderRegistry`.

---

## Consequences

**Positive**

- US-01 AC #8 (Slice 01) verifies the abstraction end-to-end via a stub provider added only in tests — no production changes required.
- Provider configuration matches the keying scheme already used by `WorkTrackingSystemConnection.AuthenticationMethodKey`; no second source of truth for "what auth methods exist."
- Adding a fourth/fifth provider in the future is a self-contained PR.

**Negative**

- `OAuthProviderNotFoundException` is a runtime error, not a compile-time error. Mitigation: a startup self-check iterates `AuthenticationMethodSchema.MethodsBySystem` and asserts every `*.oauth` key has a matching registered `IOAuthProvider`. If a key is registered in the schema without a provider, the app fails fast at boot.
- Plain string keys lose IDE refactor support relative to an enum. Mitigation: all keys live as `const string` fields on `AuthenticationMethodKeys`; using the constants restores find-all-usages.

---

## Alternatives considered

### Alternative A — Enum-keyed provider resolution

Add `enum OAuthProvider { Jira, AzureDevOps, ... }` and key the registry on the enum.

- Rejected: forces a code change (enum value addition) and an EF migration (enum-as-int column) for every new provider, contradicting story 4971's "minimal changes" goal.
- Also: `WorkTrackingSystemConnection.AuthenticationMethodKey` is already a string; an enum would create a parallel keying scheme and require synchronisation between them.

### Alternative B — Open-generic `IOAuthProvider<TConfig>` keyed by config type

`IOAuthProvider<JiraOAuthConfig>`, `IOAuthProvider<AdoOAuthConfig>` resolved by type.

- Rejected: forces every consumer (`OAuthController`, `OAuthService`) to be generic. Pollutes the call graph without adding any compile-time safety beyond the enum option, because the consumer resolves the type from a runtime string anyway.
- Also: `OAuthFlowContext` is already shared (connectionId, providerKey, scopes, redirectUri) — there is no per-provider config-shape difference that justifies generics.

### Alternative C — Direct DI keyed-service resolution (`[FromKeyedServices("jira.oauth")]`)

.NET 8 keyed services would let `OAuthController` resolve `IOAuthProvider` directly by string key with no intermediate registry.

- Rejected: keyed services scatter the resolution logic across consumers. A registry centralises (a) the not-found error message, (b) the startup self-check, (c) future cross-cutting behaviour (logging, telemetry) without touching consumers.
- Considered for future migration once the keyed-services API stabilises and the registry's value is purely indirection.

---

## References

- DISCUSS D3: "OAuth is provider-agnostic: `IOAuthProvider` port, DI-registered, configuration + credential keyed by **provider name string**"
- Story 4971: "Adding a new OAuth provider requires minimal changes"
- US-01 AC #8: stub-provider integrity test
- Existing pattern: `AuthenticationMethodSchema.cs:32-150`, `WorkTrackingSystemConnection.cs:15`

---

## Amendment — 2026-05-14: `RefreshTokenAsync` signature correction

**Status**: Accepted
**Trigger**: DELIVER step 01-07 (`feat(oauth): add JiraOAuthProvider with Atlassian 3LO flow`) surfaced that the original port signature could not accommodate real OAuth identity providers.

### Original signature (rejected)

```csharp
Task<OAuthTokens> RefreshTokenAsync(string refreshToken, CancellationToken ct);
```

### Corrected signature (accepted 2026-05-14)

```csharp
Task<OAuthTokens> RefreshTokenAsync(OAuthRefreshContext context, CancellationToken ct);

public sealed record OAuthRefreshContext(string RefreshToken, string ClientId, string ClientSecret);
```

### Why

The original signature gave provider implementations no way to access `client_id` and `client_secret` at refresh time. Every real OAuth 2.0 IdP — Atlassian Cloud (auth.atlassian.com), Microsoft Entra ID (login.microsoftonline.com), Okta, GitHub, Google — requires both credentials on the refresh-token endpoint per RFC 6749 §6 ("If the client type is confidential or the client was issued client credentials … the client MUST authenticate with the authorization server"). A provider with only the `refresh_token` cannot make a working refresh call against any of them.

The workaround proposed inside step 01-07 ("inject credentials via the named HttpClient's `DefaultRequestHeaders.Authorization` from `OAuthService.EnsureFreshTokenAsync`") would break under concurrent refresh calls: `HttpClient` is a singleton, `DefaultRequestHeaders` is shared state, and concurrent mutations are a documented race condition. The single-flight refresh design (DDD-7 + ADR-010) is explicitly built for concurrent refresh, so the workaround fails the design's load-bearing invariant.

### Scope of the correction

The change is contained:
- One new record (`OAuthRefreshContext`).
- One interface signature change (`IOAuthProvider.RefreshTokenAsync`).
- One concrete provider update (`JiraOAuthProvider.RefreshTokenAsync` — uses `context.ClientId` and `context.ClientSecret` in the form body).
- Restored two test assertions in `JiraOAuthProviderTest.cs` (the assertions dropped during step 01-07).
- No production callers exist yet — `IOAuthService.EnsureFreshTokenAsync` ships in step 02-01 with the corrected signature in mind.

### What this changes downstream

- Step 02-01 (`OAuthService.EnsureFreshTokenAsync`) will load `clientId` + `clientSecret` from the connection's `WorkTrackingSystemConnectionOption` rows (decrypted via `ICryptoService`), construct `OAuthRefreshContext`, and pass it to `provider.RefreshTokenAsync`. The OAuthService already loads the connection in `InitiateAsync` / `CompleteAsync` for the same reason — symmetric.
- Step 03-01 (`AdoOAuthProvider`) implements the same corrected signature when it ships.

### Original signature retained nowhere

The old signature is not deprecated — it is **deleted**. There are no production callers; preserving an obsolete overload would only invite mistakes.
