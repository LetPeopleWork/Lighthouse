# Wave Decisions — DISCUSS — epic-6350-bulk-reports

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-27 · **Mode**: autonomous subagent. **Predecessor**: none —
DISCOVER and DIVERGE skipped by the maintainer; the ADO Epic #6350 description and the maintainer's 2026-10-27
decisions are the inputs. **Successor**: DESIGN (`nw-solution-architect`).

Full text: `../feature-delta.md` → "Wave: DISCUSS" sections. Slice briefs: `../slices/`. Journey SSOT:
`docs/product/journeys/epic-6350-bulk-reports.yaml`. Job: `job-lead-compare-every-owner-against-its-own-past-in-one-go`
(new, `docs/product/jobs.yaml`; linked from `personas/delivery-lead-rte.yaml`).

## Config (given, not asked)

feature_type user-facing · walking skeleton "depends" → brownfield, slice 01 (read-only Reports page) · UX research
lightweight · JTBD yes (new job, every story traces to it) · density lean.

## Risk: no DISCOVER / DIVERGE

The problem rests on one informant (a consultant and early adopter: per-Team setup is a big pain; leaders want the big
picture; he scripted it). No interviews, no options explored. Accepted by the maintainer's skip. Mitigations: every
slice carries a learning hypothesis dogfooded on the dev instance; K6 asks the informant whether the script retired.

## Scope Assessment: PASS — 8 stories, 2 code bases (Lighthouse; lighthouse-clients), no new bounded context, estimated ~7¼ days

Four outcomes (find, create, remove, one PDF) in one Epic as framed by the maintainer; each slice ships alone, in one
direction of dependency (02a on 01; 02b on 02a; 03 on 02b; 04 on 02b and 03; 05, 06, 07 on 01 only). Revised after
the peer review and the re-review of 2026-10-27 (was 7 stories, ~6 days).

## Key Decisions

Locked (maintainer, 2026-10-27): **D1** same windows for every owner; owners whose kept history cannot hold them are
skipped with the reason and where to keep more history; Story 6347 Removed, not depended on · **D2** bulk creation
Premium; page and reading Community; bulk removal Community (Q1) · **D3** Teams and Portfolios in one selection ·
**D4** type-agnostic flow, only Then & Now built · **D5** bulk PDF in, last slice · **D6** clients: read-only
cross-owner list; bulk stays web-only · **D9** one name for every owner (Q2) · **D12** combined PDF in table order, no
PDF if one cannot be read, Retry / Leave it out (Q3) · **D13** owners whose history cannot hold the windows shown but
not pickable (Q4).

This wave: **D7** RBAC per owner (read to list and export, Admin to create/remove; server re-checks; RBAC/auth off =
Admin everywhere) · **D8** per-owner outcome always, for creation and removal: an owner deleted or no longer
administered mid-run is one "Not created/removed" row; a run where every owner is refused reads "0 reports created, N
not created"; a lost answer says "could not confirm" · **D11** nothing new stored · **D14** usage data wanted for page,
bulk create run, bulk remove run, several-report PDF (DEVOPS designs) · **D18** 02a's owner step offers only the Teams
the user administers (all with RBAC/auth off), so 02a is releasable alone; 02b keeps only the no-Admin state and the
owner step's loading and could-not-load looks (re-review; settles Q10).

PROPOSED — confirm at DISTILL sketch walk (a **hard gate**: no scenario pins one of these before the maintainer
confirms it there): **D9's clause** no de-duplication (Q11, never asked) · **D10** several windows = several runs (Q5,
defaulted by the main session, not answered) · ~~D13 extension~~ (dropped after DESIGN, 2026-10-27:
metrics are captured per owner kind; Q7 withdrawn) · **D15** every row selectable; Export PDF for any selection; Remove only when all selected are
removable, else disabled naming how many are not (Q6) · **D16** filter change clears the selection, select-all = rows
the filter shows on all pages, exact counts, "Pick at most 50 reports" above the ceiling N = 50 (LOCKED by the
maintainer after DESIGN), shared by the PDF, removal and the owners of one run (Q8) · **D17** Create reports always shown; Community disabled with a Premium hint; Premium without Admin
owner disabled with "You are not Admin of any Team or Portfolio" (Q9) · presets for mixed selections.

PROPOSED for DEVOPS: **D19** the bulk-run event fires for every run, including one that created nothing, with no
outcome property (K2 counts users who try bulk creation).

Slice 01 builds the selection but shows no checkboxes or "N selected" until 05 or 07 ships.

## Slices

| # | Slice | Tier | Est. | Depends on |
|---|---|---|---|---|
| 01 | Every report in one place, with row selection built but hidden (walking skeleton) | Community | ~1.5d | — |
| 02a | Create a Then & Now for several Teams at once — write path, Admin-set filter, per-owner result | Premium | ~1d | 01 |
| 02b | The owner step says why Create is unavailable (no-Admin state, loading and could-not-load looks) | Premium | ¼d | 02a |
| 03 | Portfolios join the same selection | Premium | ½d | 02b |
| 04 | The owner step says which owners cannot take the report | Premium | 1d | 02b, 03 |
| 05 | Remove reports in bulk (shows the selection) | Community (LOCKED) | 1d | 01 |
| 06 | Every report through the clients | Community | ~1d | 01 (released) |
| 07 | Several reports in one PDF (shows the selection) | Community | 1d | 01 |

02a's brief keeps the file name `slice-02-create-for-several-teams.md`; 02b's keeps
`slice-02b-owner-step-offers-admin-teams.md`.

## Constraints Established

RBAC per owner via `useRbac()`, server re-checks · same output as the single wizard · per-owner outcome · loading and
failure looks on every fetching view, the owner step included · instance terminology · no new stored data · never
colour alone.

## Upstream Changes

None to earlier documents (none exist for this Epic). Superseded for this Epic: Epic 6298's "several reports at once"
out-of-scope line (now slice 07) and D23's per-owner-only client reads (now plus a cross-owner list). Epic 5878's
documents are not edited.

## Questions for the maintainer

*Q1–Q4 answered on 2026-10-27, each (a), the recommendation; kept for the record:* Q1 licence of bulk removal
(Community) · Q2 names across owners (one name, owner beside it) · Q3 combined PDF when one report fails (no PDF, Retry
/ Leave it out) · Q4 owners that cannot hold the windows (shown, not pickable).

Open, each with a default marked PROPOSED — confirm at DISTILL sketch walk: Q5 several windows in one run (D10;
default out, two runs) · Q6 selection rules (D15) · Q7 metrics an owner does not capture (withdrawn after DESIGN) · Q8 selection
under filter and the shared ceiling (D16) · Q9 "Create reports" states (D17) · Q11 running the same bulk creation
twice (D9's clause; default no de-duplication). Q10 settled by the re-review (D18). Options in the feature delta.

## Maintainer answers (2026-10-27)

Q1 (a) bulk removal Community · Q2 (a) one name for every owner, owner shown beside it · Q3 (a) no PDF when a report
cannot be read; it names that report and offers Retry or "Leave it out" · Q4 (a) owners that cannot hold the windows
are shown but not pickable, with history start and settings link. Q5 was defaulted by the main session, not answered,
so D10 stays PROPOSED (corrected after the re-review).

## Peer review (2026-10-27)

`nw-product-owner-reviewer`: rejected pending revisions (B1–B3, H4–H8, nits 9–17), all addressed; the re-review came
back **conditionally approved**. Its conditions are applied: the Admin-set filter moved into 02a (02a ~1d, 02b ¼d); an
all-refused run (AC-2.11) and D19; D9's clause and D10 back to PROPOSED (Q11, Q5); the selection hidden until 05/07
(AC-1.9, AC-1.10); a could-not-load scenario for AC-4.7; removal shares the PDF's ceiling (AC-5.9); "N (for example
50)" in US-07; the DISTILL sketch walk stated as a hard gate for every PROPOSED item.

## DoR

9 of 9 PASS (item 1 "clear, not validated", one informant; item 6 with 01 at ~1.5d and US-02a, US-05 at the
7-scenario cap; US-02b 3 scenarios, US-04 6). Every story has an Elevator Pitch and
`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`.
