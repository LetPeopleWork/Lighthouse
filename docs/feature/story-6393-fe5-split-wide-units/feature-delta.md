# Story 6393 — FE5 Split wide units

Epic #6509 (frontend packaged by module; catalogue in Epic #6264's workspace) · workspace
`docs/feature/epic-6264-modular-monolith/` · brief
`docs/feature/epic-6264-modular-monolith/slices/slice-37-fe5-split-wide-units.md` · ADO #6393 · picked up 2026-11-12
via `/continuous-improvement before 6473`.

DISCUSS: covered at Epic level (see the Epic's Wave Decisions, D-6264-3); waves start at DESIGN.

**Why now.** The maintainer chose to run FE5 alongside Epic #6473 (settings split). Slice 05 of #6473 changes the
links and return path in `ModifyConnectionSettings.tsx`, one of FE5's units. The split of that unit should land on
`main` before #6473 slice 05 starts, so the two do not edit the same 1000-line file at once. DESIGN orders the units
with that in mind.

## Wave: PICK-UP / [REF] Inherited

Read these in the Epic's `feature-delta.md`; they are not copied here.

- `[REF] Story Map`: pick-up group G2 (can run before or alongside packaging); the dependency graph
  (`FE1 --> FE5`, done; `FE4 --> FE5` soft, not done; `FE5 <-> FE7` soft, either order).
- `[REF] Stories` → `#### FE5 — Split wide units` (the AC).
- `[REF] How to pick up a Story`: the per-Story checklist.
- `[REF] Cross-Cutting Impact Checklist`: RBAC (the shared RBAC-handler hook goes through `useRbac()`), Lighthouse-Clients,
  Website, usage data, UI sketches, Terminology.
- `[REF] Outcome KPIs + guardrails`: KPI 11 (frontend production files > 1000 lines), KPI 12 (mutation).
- `[REF] Wave Decisions`: D-6264-3/4/5, D-6264-8 (frontend track in, TanStack Query migration out).
- `[REF] Out of scope`: Team/Portfolio metrics view duplication is already generic via `BaseMetricsView`; splitting
  that file is FE5.
- `[REF] Improvement log`: the FE1 entry (Fallow, ADR-269: later frontend ratchets use Fallow rather than a
  hand-written scan; the list unit is the file pair).
- ADRs: ADR-268 (ratchet lists offenders by name, both directions, only shrinks), ADR-269 (Fallow), ADR-270 (contexts
  and layout).

## Wave: PICK-UP / [REF] DoR re-run (2026-11-12)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement, domain language | PASS | Named files and line counts below; the duplicated RBAC handlers named by function. |
| 2 Persona | PASS | `lighthouse-maintainer`, `@infrastructure`. |
| 3 ≥ 3 real examples | PASS | `BaseMetricsView.tsx` 2074, `ragRules.ts` 1063, `ModifyConnectionSettings.tsx` 1062 lines. |
| 4 UAT Given/When/Then | PENDING DESIGN | The brief's 3 AC; restated as G/W/T with the trigger DESIGN chooses for the size ratchet. |
| 5 AC from UAT | PASS | AC map to the Story's "After". |
| 6 Right-sized | PASS by deferral | Size L; DESIGN cuts it one unit per slice, at most 5 slices per nwdes feature, so more than one nwdes feature. |
| 7 Technical notes + cross-cutting | PASS | Epic's Cross-Cutting Impact Checklist; re-answered per Story in the checklist below. |
| 8 Dependencies | PASS | Soft only: FE1 done, FE4 not started (accepted: FE5 does not wait for it), FE7 either order. |
| 9 Outcome KPIs | PASS | KPI 11 baseline below, target 0 or each justified. |

## Wave: PICK-UP / [REF] Baseline re-count (2026-11-12, old → new)

Production files under `Lighthouse.Frontend/src`, tests excluded.

| Unit | Brief (2026-10-22) | Now |
|---|---|---|
| `pages/Common/MetricsView/BaseMetricsView.tsx` | 2034 | **2074** |
| `pages/Common/MetricsView/ragRules.ts` | not listed | **1063** (new over the line) |
| `components/Common/Connection/ModifyConnectionSettings.tsx` | 1201 | **1062** |
| `pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx` | 993 | 992 (under the line) |
| `pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.tsx` | 914 | 989 (under the line) |
| `hooks/useMetricsData.ts` | 970 | 970 (under the line) |
| Files > 1000 lines (KPI 11) | ≥ 2 known | **3** |
| RBAC handlers, `pages/Teams/Detail/TeamDetail.tsx` | ≈ l.144–211 | l.169–228 (`handleAssignTeamRole`, `handleRemoveTeamMember`, `handleCreateTeamGroupMapping`, `handleRemoveTeamGroupMapping`) |
| RBAC handlers, `pages/Portfolios/Detail/PortfolioDetail.tsx` | ≈ l.114–185 | l.142–205 (the four `…Portfolio…` equivalents) |

Next in line, under 1000: `services/Api/MetricsService.ts` 957 (FE4's), `FeatureSizeScatterPlotChart.tsx` 866. DESIGN
decides whether the units just under the line (992, 989, 970) are in scope or only guarded by the ratchet.

## Wave: PICK-UP / [REF] Checklist (ticked through DELIVER)

- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels unchanged. Vitest, Playwright and
      `@screenshot` green unmodified, apart from tests that move with the code they cover.
- [ ] **Ratchet in the same change**: a size rule (production files > 1000 lines, listed by name, ADR-268 style) added
      in the commit range that brings the first unit under it, and shrunk with each later unit. Never loosened.
- [ ] **Moves separate from edits**: `refactor(<area>): move …` commits apart from edit commits.
- [ ] **Re-count before designing**: done above; DESIGN re-checks if code moved since.
- [ ] **Mutation last, on frozen code**: StrykerJS score on the touched files does not drop.
- [ ] **Architecture docs in the same change**: `ARCHITECTURE.md` §12 where the frontend layout or rule changes.
- [ ] **One context at a time**.
- [ ] **Terminology**: new names follow ADR-270's glossary and the seeded default terms.
- [ ] **Cross-cutting re-check**: RBAC (shared hook through `useRbac()` / `RbacService`, no direct
      `/api/latest/authorization/my-summary` fetch), Lighthouse-Clients N/A (no wire change), Website N/A (nothing
      visible), usage data N/A (behaviour-free), UI sketch N/A unless a split needs a visible change (then stop and
      sketch).
- [ ] **No release notes** unless the maintainer says so.
- [ ] **Coordination with #6473**: the `ModifyConnectionSettings` split is on `main` before #6473 slice 05 starts.
