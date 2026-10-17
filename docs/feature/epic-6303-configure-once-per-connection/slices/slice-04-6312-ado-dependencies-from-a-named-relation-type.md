# Slice 04 — Story 6312 — An Azure DevOps Portfolio reads dependencies from the named relation end

**ADO**: Story #6312 (Epic #6303) · **User story**: US-03 (read half) · **Estimate**: ~1 day · **Depends on**: slice 03
**Release point**: yes — #6312's last slice; #6312 releases after it (L8).
**Locked decisions**: D1, D2 (no end, or no direction → nothing + warn), D3 (replaces Predecessor), D5, D5a, D6

## Goal
A Portfolio whose Dependency Field resolves to an ADO relation end reads, per Feature, the relations whose `Rel` is
exactly that end. A reference without an end reads nothing and warns. Nothing matching anywhere warns once,
listing the `Rel` values seen. Slice 03's "not yet read" warning (AC-3.9) is replaced by reading. **Story #6312 is
done after this slice.**

## Why this is thin
Every relation already arrives (`WorkItemExpand.Relations`, `AzureDevOpsWorkTrackingConnector.cs:1159-1182`), and
the waiting-end walker exists (`WorkItemExtensions.cs:54-78`) with a constant `Rel` (`:8`). The slice makes the
`Rel` the resolved one and routes the result through slice 01's selector source.

## IN
- `ExtractDependencyReferences` takes the `Rel` to match (default unchanged: `System.LinkTypes.Dependency-Reverse`).
- **Trap closed**: the relations fetch is skipped when a Parent Override is set and dependencies "do not come from
  relations" (`:1142-1145`, decided at `:648` by `ReadsTheTrackersOwnLink`). A Dependency Field resolving to a
  link type counts as coming from relations. Asserted with a Parent Override set.
- ADO "nothing matched", "no end" and "no direction" (`System.LinkTypes.Related`, D2 / CA-6) warnings on slice 02's
  surface, Refresh History (D5a).
- Docs prose drafted for `docs/concepts/worktrackingsystems/azuredevops.md` (:101-114, :203-207).

## OUT
- Display-name matching. ADO Parent Override from a relation. Linear / CSV (L4).

## Learning hypothesis
Disproves "reading a different relation type needs no extra request" if the request count rises against the
baseline.

## Production data / dogfood
Dev instance ADO project: a Portfolio named with a non-Predecessor relation actually present on its Features (from
slice 03's list; a directional relation type that slice 03's spike recorded as present on the dev organisation —
`System.LinkTypes.Dependency-Forward` at the least; Related is non-directional and reads nothing, CA-6). Measures
`OUT-6303-dependencies-recovered` for ADO the same way as slice 01: Features carrying ≥ 1 relation of the named end
to a stored Feature against Features whose `DependsOnReferences` hold it, before and after (baseline 0%, target
100%). Record both numbers here.

Baseline: 0% (no named relation read before slice 04). To be measured in the maintainer's manual review on huserben/ThisIsADemo (Tested By links 5816→5819, 5822→5825/5828).

**Result (2026-10-17, maintainer's manual review on huserben/ThisIsADemo):** reviewed and accepted. Naming `Microsoft.VSTS.Common.TestedBy-Forward` reads the 3 Tested By links as dependencies: 3 of 3, which is **100%** against the 0% baseline. Target met.

## Acceptance criteria
AC-3.4 – AC-3.8.

## Changed assumptions
(filled during DELIVER)
