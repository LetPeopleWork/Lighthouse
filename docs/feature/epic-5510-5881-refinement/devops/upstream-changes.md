# Upstream changes — DEVOPS → DESIGN artefacts (epic-5510-5881-refinement)

**From**: DEVOPS (Apex, 2026-10-02). **For**: DELIVER slice 11 to apply; ADR-217 to be amended by whoever next edits it
(the DESIGN section of `feature-delta.md` is append-only and left as written).

| Artefact | As written | Read as | Why |
|---|---|---|---|
| ADR-217 §5, DSN-13, component row `RateLimitingConfiguration.RefinementContributionPolicy` | "partitioned by subject, else by the hashed voter key, else by remote address" | "partitioned by the SHA-256 of the first presented handle — `X-Lighthouse-Voter-Key`, `X-Api-Key`, `Authorization`, the authentication cookie — else the remote address; and the address ceiling in `WhatOneAddressMayHandIn` applies to this policy too (`PermitLimit × BrowsersOneAddressMaySpeakFor`)" | `app.UseRateLimiter()` runs before `app.UseAuthentication()` (`Program.cs:225-228`), so no subject exists when the partition is chosen; every auth-on caller would share its address's partition. The presented-handle + address-ceiling pattern already exists for `UsageDataIngest`. Moving the limiter after authentication is the alternative; it changes every existing policy and is not proposed |
| Same | (no configuration named) | `RateLimits:Policies:RefinementContribution: { PermitLimit: 30, WindowSeconds: 60, QueueLimit: 0 }` in `appsettings.json`, with a test that the shipped file configures it | A policy with no configuration entry resolves to `GetNoLimiter("unconfigured")` — silently unlimited |
| DISCUSS K4 / checklist "vote cast `{refinementDay, otherDay}` + `{liveSession, async}`" | two properties | one closed enum `sizingMoment` {NoCadence, OnRefinementDay, OnOtherDay, InLiveSession} | Live is never async; one property covers every cell |
| DISCUSS K1, K2, K3, K6, K7 | per instance / per Team | per browser (proxies in `feature-delta.md` → Monitoring Contracts) | No Team or instance identity exists in the usage-data pipe, by design (ADR-191) |

No slice changes scope or order. No ADR number is consumed by this wave.
