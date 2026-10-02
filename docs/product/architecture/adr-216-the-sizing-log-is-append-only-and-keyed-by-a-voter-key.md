# ADR-216: The sizing log is append-only and keyed by a voter key — an account with authentication on, a client-held random key with it off, and a refusal when a credential belongs to no person

- **Status**: Proposed (DESIGN, 2026-10-02)
- **Date**: 2026-10-02
- **Feature**: epic-5510-5881-refinement (ADO Epic #5510 slices 11, 12, 15, 16, 17b; Epic #6137 slice 18)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Team members say Yes / Yes, but… / No about a Work Item against the SLE, with optional comments, at any time
(DD-1, D5). Votes are kept as a log and never overwritten; changing your mind and taking a vote back are log entries
(C10, DD-9). Only the voter can take their vote back (DD-10). Votes are cast from the web tab, from presenter mode
(only the facilitator's own, DD-20) and from the CLI/MCP (DD-19). A later Premium Epic (E5) compares votes with
outcomes, so the log must keep enough to do that.

Identity facts from the code:

- With authentication **off**, every request carries the same subject, `lighthouse|auth-disabled`
  (`DisabledAuthenticationHandler.cs:14-22`). A profile lookup therefore succeeds and returns one shared profile —
  "profile is null" does **not** mean "auth off". The explicit probe is `IAuthModeResolver.Resolve().Mode`.
- With authentication **on**, a cookie session or JWT bearer carries the person's `sub`. An API key carries its
  owner's `sub` only when the owner resolves (`ApiKeyService.cs:148-201`, `ApiKeyPrincipalFactory.cs:51-82`); an
  **unlinked** key carries none, and `ICurrentUserProfileService` returns `null` for it.
- ADR-165 established, for Delivery notes, that a record stores an author reference **and** the name it was written
  under, captured at write time. ADR-191 established a per-browser identity held in `localStorage` and stored only as
  a digest.
- Work Item rows are deleted and re-created by refreshes; the stable reference is `WorkItem.ReferenceId`.

## Decision

1. **One append-only table `SizingLogEntries`** (entity `SizingLogEntry`): `TeamId` (FK, cascade with the Team),
   `WorkItemReferenceId` (not a FK), `Kind` {Vote, Comment, Revocation}, `Answer?` {Yes, YesBut, No}, `Comment?`
   (≤ 2,000 chars), `VoterKey`, `VoterProfileId?` (FK, SET NULL), `VoterDisplayName` (captured at write),
   `RecordedAt` (UTC instant), `Channel`, and the yardstick the vote was cast against (`YardstickDays?`,
   `YardstickSource`, `YardstickProbability?`). Index `(TeamId, WorkItemReferenceId, Id)`. Created whole in slice 11.
2. **Nothing updates or deletes an entry.** The repository port offers `Append` and reads only. The **current vote**
   is the latest `Vote`/`Revocation` per `VoterKey` per Work Item by `Id`; `Comment` entries never change it. Taking
   back writes a `Revocation`; with no current vote it writes nothing (idempotent). Because there is no
   read-modify-write, two concurrent writes from one voter both land and the higher `Id` wins — no compare-and-set
   is needed.
3. **The voter key is derived on the server, never accepted as an identity field.**
   - Auth **Enabled**: `"account:" + sub` of the resolved person. No person (unlinked API key) → refused,
     `vote-needs-a-person`. Any name or key in the request is ignored.
   - Auth **Disabled**: the client presents a random key (≥ 32 chars) in `X-Lighthouse-Voter-Key` and a
     self-declared name (trimmed, 1–100 chars) in the body; both are **required** (`voter-key-required`,
     `voter-name-required`). Stored as `"self:" + SHA-256(key)`. The browser mints its key once into `localStorage`;
     the CLI and MCP-stdio mint one per Lighthouse URL into their local config. The name is never defaulted from an
     OS user, git config or hostname.
   - No endpoint returns a voter key; responses carry `isMine`.
4. **Channel is a declared closed enum** — `Web`, `LiveSession` (sent while presenter mode is open), `Cli`,
   `Assistant` — validated, displayed in the log and available to usage data. It is documented as declared, not
   verified: it is analytics and display, not a security boundary.
5. **No endpoint names another voter.** Presenter mode and clients can only append under the caller's own key; that
   makes "on someone's behalf" (DD-20) structurally unavailable rather than forbidden by a check.
6. **The yardstick is captured per vote** so E5 can compare a Yes with the Work Item's eventual cycle time against
   the SLE the voter was actually asked about. Nothing else is built for E5.
7. **No domain event is published yet** (no subscriber exists). The named seam for E4/E5 is a `SizingEntryRecorded`
   event published after commit by the command service.

## Alternatives considered

- **A mutable "current vote" row per voter, plus a history table.** Needs read-modify-write, a CAS
  (`ExecuteUpdateAsync` + row count) and two tables kept in step. **Rejected** — the log alone already answers
  "current" by ordering.
- **The self-declared name as the identity.** Two people with one name merge; anyone typing "Ana Lima" can take
  back Ana's vote. **Rejected.**
- **A server-minted voter key** (an extra round-trip endpoint, as ADR-191 does for consent). The consent token is a
  capability to revoke a privacy grant; this key guards nothing an auth-off instance does not already grant to
  anyone (D11), so the extra endpoint buys nothing. **Rejected.**
- **A FK to the Work Item row.** A refresh can delete and re-create the row, cascading or orphaning votes.
  **Rejected** — `ReferenceId` survives.
- **Attributing an unlinked API key's vote to the key's creator name.** Fabricates a person from a string match.
  **Rejected** (DD-19 a).
- **Inferring the channel from the authentication scheme.** Cannot separate a browser from the CLI on an auth-off
  instance. **Rejected.**

## Consequences

- **Positive**: one write shape for web, presenter and clients; no lost updates by construction; E5 is not
  precluded.
- **Positive**: the auth-off identity is good enough for its purpose (keep honest people's votes apart and give
  "take back" a meaning) without pretending to be authentication.
- **Negative**: clearing browser storage, switching browsers or turning authentication on makes the same person a
  new voter. Documented; it can double-count a person on a Work Item that stays in refinement across the change.
- **Negative**: the log grows forever, by design (E5). At a few thousand entries per Team per year it is small; it
  goes with the Team (cascade).
- **Negative**: `Channel` can be mis-declared by a hand-written client. Accepted — it is not used for authorisation.

## Enforcement

- `ISizingLogRepository` declares no `Update*`/`Remove*`/`Delete*` member; no code calls `ExecuteUpdate*`,
  `ExecuteDelete*` or `Remove` against `SizingLogEntries` (ArchUnitNET + reflection).
- `VoterIdentityResolver` branches on `IAuthModeResolver`, pinned by a test where auth is off **and** the shared
  profile exists (the case a profile-null check gets wrong).
- Response DTOs contain no member named or typed as a voter key (reflection test).

Cross-refs: [ADR-137](./adr-137-viewer-identity-embed-session.md),
[ADR-165](./adr-165-delivery-note-authorship-and-the-absent-profile.md),
[ADR-190](./adr-190-usage-data-events-detected-in-the-browser-forwarded-by-the-backend.md),
[ADR-191](./adr-191-analytics-identity-is-a-per-browser-pseudonym-never-on-the-wire.md),
[ADR-217](./adr-217-a-sizing-vote-is-a-write-gated-by-team-read-through-a-named-requirement.md).
