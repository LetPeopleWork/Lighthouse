# Slice 01 — the query field says what to put in it

**Story**: A (`../feature-delta.md`) · **ADO**: #5610 · **Effort**: ~0.5 day
**Order**: first (D1). Reaches the persona who actually hits the wall; needs neither the SPIKE nor 5611.

## Goal

A flow coach facing the blank ServiceNow query field is told what the field wants, shown a real
example, and pointed at where ServiceNow itself will hand them one.

## Learning hypothesis

**Confirms** that the slice-02 dogfood wall was *ignorance, not absence* — that the encoded-query
concept is fine once someone says what it is, and that a picker is a convenience rather than a
prerequisite.
**Disproves** it if a guided coach still cannot author a query that passes `ValidateTeamSettings`. That
outcome would make slice 02 mandatory rather than optional, and would also cast doubt on ruling R-2's
accepted false-positive cost — a guard nobody can satisfy is not a guard.

## IN scope

- A new **nullable help field on the data-retrieval schema** (D3), in **both twins** (D4):
  `DataRetrievalSchemaDto.cs` and `DataRetrievalSchemaDefaults.ts`. Null for every connector except
  the ServiceNow **team** row.
- Rendering it on the existing `TextField` in `GeneralSettingsComponent.tsx:160-182` as placeholder
  and helper text. One component change, all connectors, no per-connector branch.
- The ServiceNow copy itself: what an encoded query is, a worked example, and the two silent-failure
  modes (AC-A4) — an unknown *field name* is dropped and the query widens to the whole table; a bad
  *value* on a real field silently matches nothing.
- Both surfaces: `ModifyTeamSettings` and `CreateTeamWizard`, which share the component.

## OUT of scope

- The docs page and the *Copy query* walkthrough — #5578 (D2).
- The board picker — slice 02.
- Touching `wizardHint` (D5), `inputKind` (D8), or the portfolio schema (AC-A6).
- Rewording `ValidateTeamSettings`' verdicts. R-2's messages stand; this slice puts guidance *in front*
  of the guard, it does not soften it.

## Acceptance criteria

AC-A1..AC-A6 in `../feature-delta.md`.

## Dependencies

- **D12 — #5611 delivered.** The only blocker; nothing in this slice's content needs it.
- Bug #5613's schema-twin guard in place (shipped, `cb5f0efb0`) — D4 rides on it.
- The copy should be checked against what #5578's page will say, so the two do not diverge on day one.

## Reference class

- `DataRetrievalSchemaDefaults.ts` / `DataRetrievalSchemaDto.cs` — the twin pair, five connectors ×
  two contexts. Adding one nullable field is 20 rows of mechanical change plus one real value.
- `DataRetrievalSchemaDtoTest.cs` — the existing per-connector assertions; extend before editing the
  contract, per the project's shared-contract rule.
- `GeneralSettingsComponent.tsx:107-126` — where `schema` is already destructured for `displayLabel`,
  `inputKind` and the read-only computation. The help field joins that group.

## Pre-slice SPIKE

**None.** Nothing here is an instance fact; the two failure modes the copy describes were already
measured in `docs/feature/epic-5513-servicenow-integration/spike/findings.md` (Q3) and re-confirmed by
the slice-04 dogfood (`Correlation ID` vs `correlation_id`, 103 rows vs 36).

## Dogfood moment

Same day, and it must be the **repeat of the original walkthrough**: create a ServiceNow team from
scratch without leaving the product to look anything up. The KPI is that it completes, and that no
`query_matches_whole_table` refusal is hit on the guided path.
