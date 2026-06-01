# ADR-045: Per-instance nudge settings (`installTimestamp` + `lastShownAt`) on the existing AppSettings mechanism

> **Scope: LIGHTHOUSE repo (`/storage/repos/Lighthouse`) — C# .NET 8 backend (hexagonal) + frontend read.** Authored for ADO Epic #5124, US-06/US-07.

## Status

Accepted (DESIGN wave, 2026-05-31)

## Context

The FE-derived eligibility (ADR-044) needs two authoritative, server-side per-instance values:

- **`installTimestamp`** — written ONCE on first run, never reset by restarts; drives the ~2-week gate.
- **`lastShownAt`** — written on nudge show/dismiss; drives the ~6-month cadence.

Lighthouse already has a per-instance settings mechanism: `AppSetting` entity, `AppSettingKeys` constants, `AppSettingService` (`IRepository<AppSetting>`-backed, `GetByPredicate(key)`), `IAppSettingService`, and `AppSettingsController` (`api/v1|latest/[controller]`). EF migrations are generated via the existing `CreateMigration` PowerShell script across **both** providers (Sqlite + Postgres) — never `dotnet ef migrations add` directly.

**Constraint discovered in code**: `AppSettingsController` is `[RbacGuard]`-protected. The nudge is shown to a **non-premium community user who is NOT an admin** and must NOT require RBAC admin to read its own install age. So the settings READ for the nudge must be reachable by a normal authenticated app user, not gated behind admin RBAC.

## Decision

**Extend the existing AppSettings mechanism with two new keys; expose a narrow, non-admin read + a `lastShownAt` write; do NOT invent a new persistence subsystem.**

- Add to `AppSettingKeys`: `SurveyNudgeInstallTimestamp = "SurveyNudge:InstallTimestamp"` and `SurveyNudgeLastShownAt = "SurveyNudge:LastShownAt"`. Values are **ISO-8601 UTC instants** (string), parsed UTC-stable on read.
- Extend `IAppSettingService`/`AppSettingService` with: `GetSurveyNudgeSettings()` (returns `{ installTimestamp, lastShownAt }`, both nullable), `EnsureInstallTimestamp()` (first-run write-once — sets it to `DateTime.UtcNow` only if absent; idempotent, never overwrites), and `RecordNudgeShown(DateTime utcNow)` (sets `lastShownAt`).
- **First-run write-once**: `EnsureInstallTimestamp()` is invoked once at startup (composition root / app-init seam, alongside the existing settings seeding). It writes only if the key is absent, so restarts never reset it (journey integration checkpoint "set exactly once per instance and never reset by app restarts").
- **Endpoint exposure WITHOUT widening RBAC**: a small read surface for `{ installTimestamp, lastShownAt }` and a write for `RecordNudgeShown` that is reachable by a normal authenticated app session (the audience that sees the nudge), NOT `[RbacGuard]`-admin. Concretely: a dedicated `SurveyNudgeController` (or a settings sub-route) carrying the app's standard authenticated-user policy but NOT the admin `[RbacGuard]`. **This read is part of the existing settings mechanism's surface, so per ADR-044 it does NOT count as a new "feature endpoint" that the CLI/MCP clients consume — clients remain N/A.** (If the user instead chooses ADR-044 Option (b), this controller would additionally host the server-side eligibility evaluation and THEN the clients version-gate applies.)
- **EF migration** via `CreateMigration` PowerShell script for Sqlite + Postgres. The two settings are scalar string rows in the existing `AppSetting` table — no new table; the migration only seeds defaults if the seeding pattern requires it (install timestamp is written lazily on first run, not seeded).

### Earned Trust — the settings store is a driven adapter; probe it

The `installTimestamp`/`lastShownAt` persistence is a **driven adapter** over EF/the database — and the whole nudge correctness (premium never bothered, ~2-week gate, ~6-month cadence) rests on it honoring two properties the database environment could lie about:

1. **Write-once durability** — `EnsureInstallTimestamp` must persist across restarts. If the underlying store silently no-ops a write (a known class of substrate lie — e.g. an in-memory or read-only DB, a misconfigured provider), the timestamp would be re-derived on every boot and the ~2-week gate would never elapse, OR be re-derived as "now" each boot and never fire — both correctness failures.
2. **Read-after-write monotonicity** — a value written must read back equal (no clock-rewrite, no UTC/local coercion by the provider).

Per principle 12, the design includes a **startup probe** for this adapter at the composition root (the existing settings-seeding seam): on init, write a sentinel settings round-trip (or assert the install-timestamp key is present-and-stable after `EnsureInstallTimestamp`) and verify read-back equality + UTC kind. The nudge feature's eligibility treats a failed/uncertain settings read as **not eligible (fail closed)** — consistent with ADR-044 — rather than refusing app startup (the nudge is non-critical; a settings-store fault must NOT prevent Lighthouse from running its core flow-metrics job). This is the Earned-Trust answer to "what happens if the environment lies?": the lie degrades to *no nudge*, never to *a premium user nagged* or *a fired-on-day-0 nudge*. The probe is covered by an integration test (EF InMemory + a write-then-read assertion) and the deterministic premium-guard test (ADR-044).

## Alternatives Considered

- **A new dedicated table / entity for nudge state**: rejected — the per-instance `AppSetting` key/value store already models exactly "one scalar setting per instance"; a new table is unjustified for two scalars (CLAUDE.md simplest-solution, reuse-first).
- **Expose the values through the existing `[RbacGuard]` `AppSettingsController`**: rejected — that controller is admin-gated; a non-admin community user (the nudge audience) would be 403'd reading their own install age. A non-admin authenticated read surface is required.
- **Store `installTimestamp` client-side**: rejected — see ADR-044 Option (c); the value must be authoritative and restart/clear-stable, which only server persistence gives.
- **Compute install age from an existing first-seen signal (e.g. first work-item sync, license import date)**: considered and rejected for v1 — those signals are not guaranteed present on a fresh community instance and conflate "installed" with "configured"; an explicit write-once `installTimestamp` is unambiguous. (Flagged as a possible future simplification if such a signal proves reliably present.)

## Consequences

- **Positive**: reuses the existing AppSettings mechanism (no new subsystem); write-once install timestamp is restart-stable; values are UTC ISO-8601 so the FE comparison is UTC-stable (ADR-044); the settings read is reachable by the nudge's actual non-admin audience; a startup probe makes the persistence guarantee empirical, with a fail-closed degradation that can never bother a premium user.
- **Negative**: a small non-admin read surface is added (bounded; not a CLI/MCP feature endpoint under Option (a)); the `CreateMigration` script must be run for both providers (operational discipline, per CLAUDE.md).
- **Cross-repo / clients**: under ADR-044 Option (a) the CLI/MCP clients are **N/A** (no feature endpoint). Recorded for the DEVOPS handoff.
