# Slice 03 — Rotate the key without asking anyone for a token

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5778 · **Story**: US-03 · **Estimate**: probe 2h, then ~6h (re-cut after the probe)
**Reference class**: none in this repository. The nearest thing is the OAuth token refresh path, which
rewrites the same `OAuthCredential` rows a rotation must walk — which is why the probe below exists
rather than an estimate that pretends the risk away.

## Goal

An administrator moves every stored secret onto a new key from inside Lighthouse, and not one
credential is re-entered.

## Pre-slice SPIKE (timeboxed, 2h, runs BEFORE the brief is committed to)

**Question (OQ-1)**: does an in-place re-encryption pass need to hold off the sync pipeline, or is
per-row optimistic concurrency enough?

The concrete hazard: `OAuthService.PerformRefreshAsync` rewrites `AccessToken` and `RefreshToken` while
a rotation is walking those same rows. A lost update here is not a stale field — it is a credential
nobody can recover without re-authorising with the tracker, which is precisely the cost this slice
exists to remove.

Probe: drive a refresh and a rotation at the same rows concurrently on `:5169` and observe whether the
existing concurrency token catches it. **If a lock is required, this brief is re-cut before dispatch** —
that is a design change, not a code tweak, and it goes back to DESIGN.

## IN scope

- Generate a new key, make it active, retire the previous one **without removing it from the ring**.
- Re-encrypt every readable stored secret under the new active key: connection options flagged
  `IsSecret`, and OAuth access and refresh tokens.
- Resumable and idempotent. Interrupted halfway, the instance still works — both keys are in the ring —
  and running it again finishes the remainder.
- A secret that cannot be read is **left byte-for-byte untouched** and named in the report. Rotation
  never overwrites what it could not verify.
- A report: how many re-encrypted, how many unreadable, per Connection.
- System Admin only, recorded — who, when, how many. No key material anywhere in the record.
- On an instance still on the published default, rotation removes the last row referencing it and the
  default leaves the ring.
- **A custody-aware driving surface.** Minting and re-encrypting are separate jobs and only
  re-encryption is always the application's. The panel names the custody mode, lists the key ids the
  ring holds, and offers:
  - **Rotate key** where the application owns the key (standalone, Docker) — mint, activate,
    re-encrypt, retire, in one action.
  - **Re-encrypt onto the active key** where an operator owns it (a Kubernetes Secret, chart-made or
    External Secrets / OpenBao-owned). The operator adds the new key to the Secret alongside the old
    and rolls the pod; Lighthouse re-encrypts. It never mints a key it cannot persist, so it needs no
    write access to a Secret and does not fight an external store's next sync.
  Re-encryption is one code path in both modes. Only minting differs.

## OUT of scope

- Scheduled or automatic rotation.
- The read-only readability check (slice 04) — though the report shares its vocabulary deliberately.
- Provisioning a key into a Kubernetes Secret, and the chart plumbing for it (slice 05). This slice
  ships the re-encryption both custody modes use, and the panel that adapts to them.
- Re-keying hashed values. API keys and embed secrets are hashed and are not affected by any of this.

## Learning hypothesis

**Disproves** "in-place re-encryption is safe without holding off the sync pipeline" if a concurrent
token refresh loses an update or leaves a row unreadable. Falsified by the probe, cheaply, before the
implementation exists.
**Confirms**, if it holds, that the epic can offer rotation as an ordinary operator action rather than
a maintenance window.

## Acceptance criteria

AC-3.1 through AC-3.13 in `feature-delta.md`. The four that carry the slice:

- **AC-3.3** — no credential is requested, re-entered or invalidated; every Connection works
  immediately afterwards.
- **AC-3.5** — an unreadable secret is left untouched and named. Never overwritten.
- **AC-3.7** — a token refresh landing mid-rotation neither loses the refreshed token nor leaves the
  row unreadable.
- **AC-3.11** — where an operator owns the key, the panel offers re-encryption and does not offer to
  mint. Lighthouse never writes to a Kubernetes Secret.

## Dependencies

- Slice 01 (a read that can fail, and a key id per secret) and slice 02 (a ring with more than one key).
- `:5169` restored from a real backup, with live OAuth credentials, for the probe and for AC-3.7.

## Dogfood moment

Same day: rotate the restored `:5169` instance while a sync is running, then trigger every Connection
and confirm none asks for a credential. Record the duration against KPI-3.

## Verdict

**The probe, 2026-08-16 — no lock. The brief was not re-cut.**

Measured against real SQLite arranged the way `DatabaseConfigurator` arranges it (WAL,
`busy_timeout=10000`) and a real PostgreSQL in a container, not against a fake and not against the
in-memory provider. A guarded update that names the ciphertext it observed reports zero rows affected
when a token refresh rewrote the row in between, on **both** providers, and the refreshed token
survives. A row nobody touched is moved. `OAuthService`'s per-connection gate is an in-memory
dictionary on a singleton (`OAuthService.cs:184`), so it coordinates nothing across replicas and no
in-process lock could ever have been the answer.

Committed as `Lighthouse.Backend.Tests/Integration/Containers/ReEncryptionCompareAndSwapProbeTests.cs`
rather than thrown away: it is the evidence behind ADR-151's first Earned Trust row, on both providers,
in CI. Fallbacks B and C are unused, so DESIGN F-6's condition does not fire and the slice ships no EF
migration.

**Learning hypothesis: confirmed.** Rotation is an ordinary operator action rather than a maintenance
window.

**Slice verdict**: _to be recorded after the dogfood pass on `:5169` — rotate while a sync is running,
trigger every Connection, confirm none asks for a credential, and time it against KPI-3._

**One thing narrowed rather than delivered**: AC-3.9's second half. Re-encryption moves the last secret
off the key published with the product permanently, and the live ring stops holding that key — but the
next start appends it again, because the ring is assembled without asking the database. Recorded with a
recommendation in `feature-delta.md` under *Wave: DISTILL / [REF] Upstream Issues*, for slice 06.
