# Slice 03 — Move to Top / Up / Down / Bottom

**Feature**: epic-5375-manual-sorting · **ADO**: Epic #5375 · **Story**: US-03 · **Estimate**: ~6h
**Reference class**: none clean on the backend — this is the first write surface on `FeaturesController`
(S13). On the frontend, the existing row-action patterns in `FeatureListDataGrid` are the model.

## Goal

A product owner picks **Move to Top** on a Feature they own, from either the Features view or their
Portfolio's list, and the forecast sequences their delivery accordingly — without being able to touch a
Feature they do not own.

## IN scope

- **Rank service** implementing insert-at-target (D4): place X at the global rank currently held by the
  target row, shift the block between old and new position by one, renumber densely (D13). This is the
  epic's one shared abstraction and it ships **here**, inside its first consumer; slice 04 reuses it
  unchanged.
- `PATCH api/v1|latest/features/{featureId}/rank`, body carrying exactly one of `beforeFeatureId` /
  `afterFeatureId`, with `beforeFeatureId: null` meaning "to the end".
  `[LicenseGuard(RequirePremium = true)]` + RBAC per D11: `PortfolioWrite` on **every** Portfolio the
  moved Feature belongs to (S7 — a Feature can be in several).
- All four relative actions map onto that one endpoint shape: Top → before the first visible row; Up →
  before the previous visible row; Down → after the next visible row; Bottom → `beforeFeatureId: null`.
  **Visible**, not absolute — hidden Done Features (D15) and filtered rows are jumped, not landed on.
- Row-action menu in the shared `FeatureListDataGrid`, so both surfaces get it from one change (D10).
- Disabled states with explanatory tooltips: no `PortfolioWrite` (AC-3.7); Feature shared with a
  Portfolio the user cannot write (AC-3.8); grid sorted by a column (AC-3.9, D14); manual sorting off or
  non-premium (AC-3.10).
- Keyboard operation and screen-reader announcement of the outcome (AC-3.11) — asserted, not assumed,
  because accessibility is a stated reason for choosing buttons over drag (D18).
- Optimistic reorder in the grid, reconciled against the server response.
- Transaction boundary such that a concurrent work-item refresh cannot interleave with a renumber.

## OUT of scope

- "Move above/below Feature X" and its picker (slice 04).
- Drag-and-drop (D18 — deferred, not planned).
- Bulk operations, multi-select.
- Any write-back to the tracker (D8).

## Learning hypothesis

**Disproves D4 itself** if a move made from a Portfolio's filtered list produces a global order the user
finds surprising. The known sharp edge: a Portfolio's Features are generally **non-contiguous** in the
global order, so **Move to Top** from Portfolio A lands the Feature above A's own first Feature — not at
global rank 1 — and in doing so crosses Features that are not in this Portfolio and not on this screen.
Insert-at-target causes exactly one such crossing; the rejected alternative (slot permutation) causes two.

If one crossing still reads as wrong to the person doing it, the fallback is slot permutation, and the
slice is re-scoped rather than abandoned — only the rank service's body changes; the endpoint, the RBAC,
the menu and the disabled states all survive a change of translation rule.

**Confirms**, if it holds, that slice 04 can add a second gesture over the same rule without re-opening
the question.

## Verify the premise first (before writing the rank service)

**Find a shared Feature.** AC-3.8 and D11 both hinge on a Feature belonging to more than one Portfolio.
Slice 01 seeds one in demo data; confirm the *dogfood* instance also has a real one. If it does not,
D11's most interesting case ships validated only by an integration test — say so out loud rather than
discovering it at DELIVER.

## Acceptance criteria

AC-3.1 … AC-3.11 verbatim from `feature-delta.md`. The three that carry the slice:

- Global `F1 F2 F3 F4 F5 F6`, Portfolio A = `{F2, F4, F5}`, **Move to Top** on F5 from A's list →
  `F1 F5 F2 F3 F4 F6` (AC-3.2). This single assertion is D4, and it is the one that pins "top of *my*
  list" against "top of the instance".
- Moving a Feature into the top `FeatureWIP` positions improves its 85% date by ≥1 day and worsens the
  displaced Feature's (AC-3.6). **The fixture must carry variable throughput** — constant-throughput data
  cannot demonstrate a sequencing effect at all (Epic 5459's lesson,
  `project_epic5459_multi_team_forecasts_resume`).
- A user with write on A but not on B cannot move a Feature belonging to both: actions disabled, and 403
  from the endpoint (AC-3.8).

## Dependencies

Slice 01 (the Features view, the shared column, demo data). Slice 02 (`ManualRank`, the switch, the
single comparison selection point). DESIGN open questions 3 (renumber transaction) and 5 (D11 strictness).

## Dogfood moment

Same day: on a real multi-Portfolio instance, Move to Top the Feature everyone has been complaining
about, trigger a forecast run, watch the dates move. Then check what happened to a Feature in a Portfolio
you did **not** open — that is the D4 verdict, and it is the whole point of this slice. Start counting
for K7 from this day: how often is the answer "Move to Top" versus a run of Move Ups?
