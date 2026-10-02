# Wave Decisions — DESIGN — epic-5510-5881-refinement

**Agent**: Morgan (`nw-solution-architect`) · **Date**: 2026-10-02 · **Interaction mode**: PROPOSE (autonomous subagent)
**Predecessor**: DISCUSS (Luna; DD-1..DD-22; D1–D31 settled). **Successor**: DEVOPS (`nw-platform-architect`), then
DISTILL (`nw-acceptance-designer`).
**Scope** (maintainer, 2026-10-02): FULL for E1 (#6136, slices 01–02), E2 (#5881, 03–09), E3 (#5510, 10–17b);
LIGHT for E4 (#6137, slice 18; spike 19 questions only); E5 (#6138) out but not precluded.

## Config

Paradigm OOP (unchanged, per CLAUDE.md) · style modular monolith + ports-and-adapters (ADR-027, unchanged) ·
C4 L1 + L2 + L3 (the Refinement module has more than five components) · no new technology.

## Architecture in three lines

1. Refinement settings = one JSON-valued property on `Team`, saved through the existing Team settings write (ADR-214).
2. Everything on the tab is derived on read; the need band is the manual forecast's How Many for the next Refinement,
   read at (100 − p) (ADR-215).
3. An append-only sizing log keyed by a server-derived voter key, written under a named read-level requirement, with
   one pure resolution for stage, readiness and the hidden split (ADR-216, ADR-217, ADR-218).

## Decisions taken in this wave

Full text: `feature-delta.md` → "Wave: DESIGN / [REF] Decisions (DSN-n)".

- **DSN-1** New module `Refinement`; only `API` depends on it.
- **DSN-2** `RefinementSettings` value object on `Team`, one JSON column (vs per-setting columns, vs 1:1 table).
- **DSN-3** Saved via `PUT /teams/{teamId}`; null = unchanged; `WorkItemRelatedSettingsChanged` ignores it.
- **DSN-4** States picked from To Do ∪ Doing, mapping-aware via `GetRawStatesForCategory`; stale entries flagged.
- **DSN-5** `TeamDto.refinementConfigured` drives the disabled tab.
- **DSN-6** Backlog order = tracker `Order` via `FeatureComparer.CompareOrderValues` (corrects AC-2.1).
- **DSN-7** Next Refinement date from a `WeeklyRecurrence` extracted from the blackout rule; instance zone.
- **DSN-8** Need band = manual-forecast How Many; value at p = `GetProbability(100 − p)`.
- **DSN-9** Verdict Below / In / Above or a closed unavailable reason; facts only on the wire.
- **DSN-10** SLE yardstick, fallback P85 of default cycle time over the Throughput window (corrects AC-10.1).
- **DSN-11** `SizingLogEntries` append-only table, all columns in slice 11; keyed by `ReferenceId`.
- **DSN-12** Voter identity from `IAuthModeResolver`; account / self-declared name + client voter key / refuse.
- **DSN-13** `RbacGuardRequirement.TeamContribute` = Team read predicate, 404 non-disclosing, rate-limited.
- **DSN-14** DD-5 precedence pinned: rule decides stage and readiness; overlapping rules Ready > Being refined > Waiting.
- **DSN-15** Current vote = latest Vote/Revocation per voter; open question until the asker votes; take-back idempotent.
- **DSN-16** Hidden split enforced in the API; presenter reveal needs `TeamWrite` (MQ-2).
- **DSN-17** Channel = declared closed enum {Web, LiveSession, Cli, Assistant}.
- **DSN-18** No domain event yet; `SizingEntryRecorded` named as the E4/E5 seam.
- **DSN-19** Read/write driving ports and controllers split.
- **DSN-20** Terminology keys `refinement`/`refinements` seeded in slice 01.
- **DSN-21** CLI `lh refinement …`, `lh config voter set`; MCP `lighthouse_team_refinement_*`.
- **DSN-22** Each vote captures its yardstick (E5 enabler, nothing else built for E5).

**DD-19 verdicts**: (a) resolve the credential to a person or refuse (`vote-needs-a-person`); (b) auth-off name is
required — CLI `--as` or an explicitly stored name, MCP `voterName` argument, never defaulted; per-client voter key in
local config; mcp-http refuses auth-off votes; (c) client votes are marked `Cli`/`Assistant`; (d) the MCP tools tell
the assistant to confirm answer and comment with the user before calling.

## ADRs

| ADR | Title |
|---|---|
| [214](../../../product/architecture/adr-214-refinement-settings-are-one-json-valued-property-on-the-team.md) | Refinement settings are one JSON-valued property on the Team, saved through the Team settings write, in a module of their own |
| [215](../../../product/architecture/adr-215-the-need-band-is-the-manual-how-many-for-the-next-refinement-read-at-100-minus-p.md) | The need band is the manual forecast's How Many for the next Refinement date, read at (100 − p) |
| [216](../../../product/architecture/adr-216-the-sizing-log-is-append-only-and-keyed-by-a-voter-key.md) | The sizing log is append-only and keyed by a voter key |
| [217](../../../product/architecture/adr-217-a-sizing-vote-is-a-write-gated-by-team-read-through-a-named-requirement.md) | A sizing vote is a write gated by Team read, through a named `TeamContribute` requirement |
| [218](../../../product/architecture/adr-218-stage-readiness-and-the-hidden-split-are-one-pure-resolution-on-read.md) | Stage, readiness, the open question and the hidden split are one pure resolution on read |

Numbered from 214: this worktree's highest was 212 and the main checkout holds an uncommitted 213 (Story 6131).

## Migration plan

M1 (slice 01): `Teams.RefinementSettings` nullable JSON column. M2 (slice 11): `SizingLogEntries` table, every
column. No other slice migrates. Both via `CreateMigration`, SQLite + Postgres, expand-only.

## Artefacts

| Path | What |
|---|---|
| `feature-delta.md` → `## Wave: DESIGN / …` | decisions, components, ports, reuse, data model, RBAC, identity, per-slice notes, enforcement, changed assumptions, open items |
| `design/upstream-changes.md` | AC/story wording DISTILL should apply |
| `docs/product/architecture/adr-214..218-*.md` | five ADRs |
| `docs/product/architecture/brief.md` → `## Application Architecture — epic-5510-5881-refinement` | brief section |
| `docs/product/architecture/c4-diagrams.md` → `# C4 Architecture Diagrams — epic-5510-5881-refinement` | L1, L2, L3 |

## Handoff

- **DEVOPS**: design the usage-data events against K1–K7 from the exposed facts (`verdict`, `unavailableReason`,
  `isRefinementDay`, the write response's row readiness, `channel`); `TeamTabOpened` + route key `refinement` (DD-16).
  No new infrastructure, no external integration (contract testing N/A).
- **DISTILL**: apply `upstream-changes.md`; add the refusal paths as error scenarios; the 300-row timing and the
  band/manual-forecast parity are acceptance checks.
- **DELIVER**: `ARCHITECTURE.md` gains the eighth module, the sizing log as a persistence shape and the reader-write
  rule, in slice 01/11's own changes.

## Maintainer questions

MQ-1 forecast filter in the need number · MQ-2 presenter split reveal to Team admins only · MQ-3 mcp-http refuses
auth-off votes · MQ-4 no age on To Do rows. Defaults are chosen and documented; none blocks DISTILL.

## Peer review

See the end of the DESIGN sections in `feature-delta.md` ("Peer review").
