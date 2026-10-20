# DISTILL wave decisions — Epic #6298 Report PDF Export

2026-10-19. `[lang-mode] csharp + typescript` · `[policy-mode] inherit` (four rows appended) · `[port-mode] n/a`
(this repository asserts state through NUnit, Vitest and Playwright; there is no `state_delta` port).

## Reconciliation

**Passed: 0 contradictions** across DISCUSS, DESIGN and DEVOPS. Every apparent conflict had already been settled
upstream, and the later decision is the one pinned: D20 → NMD-2 (the list icon), AC-1.8 → U-1, D28 / AC-1.10 →
Q2 / NMD-7, US-03's "one line" → NMD-5's three lines, DEVOPS "pin the name, never the integer" → append order.

**Revised after review 2026-10-19.** Re-run against the revised DESIGN (DDD-24..32, PD-1..PD-6), DISCUSS (U-8..U-16)
and DEVOPS: still 0 contradictions. PD-1..PD-6 were defaults chosen while the maintainer was away; the
maintainer confirmed all six on 2026-10-20, so the scenarios stand as written.

**Second revision 2026-10-19.** Re-run against DESIGN's second revision (N1–N8, PD-6) and U-16: still 0
contradictions. The page's fonts are driven through a `loadFonts` stand-in handed to `reportPrintRoutes`, never a
faked `document.fonts`; the production binding (`browserPrintEffects`) has its own tests that stub only the browser's
print and fonts; the return-from-print marker is a named value (`RETURNED_FROM_PRINT`) the tests assert. Details in the
DISTILL sections of `feature-delta.md`.

## Decisions

| # | Decision | Why |
|---|---|---|
| DS-1 | Frontend scenarios render the real Reports pages through one kit (`src/tests/ReportPrintTestKit.tsx`, `renderTheExportScene`): real routes, `TerminologyProvider`, `useRbac`, TanStack cache and consent provider. Only the backend's answers, the print dialog and font loading are stood in | Pillar 3: the page is the one a reader runs; what is faked is what jsdom does not have |
| DS-2 | Frontend scaffolds: `print/reportPrintRoutes.tsx`, `print/printPageState.ts` and `print/usePrintOnce.ts` throw `SCAFFOLD:`; `print/printReadPort.ts` is a type only. Export PDF, the list icon, the drawn marks and the bundled typeface are RED by being absent | These are the seams the tests must name to reach the page, its pure state and its print-once hook |
| DS-3 | The ready predicate is pinned through `data-render-ready` ("false" while preparing, "true" when ready). Readiness is tested on a directly loaded page; printing once is tested after Export PDF | A pasted link never prints, so readiness and printing can only be told apart this way |
| DS-4 | A successful Retry after a failed fresh read prints once (PD-1, **confirmed 2026-10-20**; AC-1.11). Retry on a directly loaded page reads again and never prints | Was left unpinned in the first pass; DESIGN has since decided it |
| DS-5 | A renamed Cycle Time panel is pinned as the term alone ("Lead Time") (PD-2, **confirmed 2026-10-20**) | DDD-8 reuses the screen's panels, and their titles are the term alone |
| DS-6 | `ReportPdfExportStarted` is pinned by name, by being appended after `WorkTrackingSystemFieldsChanged`, and by every earlier name keeping its number (0–19). The next free integer on this checkout is **20** | DEVOPS: tests pin the name, never the integer |
| DS-7 | 29 scenarios that hold today are **active guards**, not pending: no Export PDF on a report that failed or of an unknown Type and nothing reported for it, the read port's three members, the page-state module importing no framework, a return to the list counting as a Reports-tab opening, the print address names no page, punctuation stays text, no how-to-read lines on screen, the usage page publishes no print address, earlier event numbers, other events still refusing a type, and the clients' "reports nothing" / bad-flag cases | They would pass vacuously if pending, which is no RED |
| DS-8 | Clients: the recorded answers in `test-support/reportAnswers/` are written by hand to the wire shape (`ReportRead` / `ReportList`). Facts are compared by deep equality with the server's answer, whatever label the facts block uses. The summary's dates are accepted as ISO or "d MMM" | No dev-instance capture is committed (no customer data in git); the label and date format are DELIVER's |
| DS-9 | Clients: the refusal wording is pinned by fragment: "no access" plus the owner term, "newer version" and "can't be shown here", "no such report", "Upgrade Lighthouse" | DDD-19 decides the meaning; no full sentence was pinned by the maintainer |
| DS-10 | The skill's twelve eval cases wait at `test-support/pending-skill-evals/lighthouse-reports/cases.awaiting-fixtures.json`, not `cases.json`, so the active evals test stays green. They move into `skills/lighthouse-reports/evals/` with fixtures captured from the dev instance in DELIVER | The evals test requires every fixture to exist and story ∈ {6245, 6217, 6246, 6247} |
| DS-11 | E2E: one walking skeleton (Export PDF → the print page names the owner and the report → prints once → back on the report) and one font check (slice 02), both `testWithDemo.fixme` | E2E stays a thin sanity check; chrome, branding and the windows line are pinned in Vitest |
| DS-12 | Slice 03's "How to read this" pins the drawn marks (two icons), not the ✔ ✘ characters | Slice 02 lands before slice 03 |
| DS-13 | The typeface guard requires **every** font stack naming Quicksand to name "Quicksand Variable" first | Three components ask for "Quicksand, sans-serif" today and would fall back to a machine font |
| DS-14 | The print page's state is pinned twice: as a pure table over `printPageStateOf` (fresh = `dataUpdatedAt ≥ mountedAt`; 404 / 422 final and winning; any other read error or a failed font load retryable) and through the page | The table states the rule once; the page tests show the reader sees it |
| DS-15 | Printing once is pinned on `usePrintOnce` with a stand-in browser port (replace the entry, then print; StrictMode; redraws; Retry; the after-print return by origin) and on the page (reload or Forward onto the used-up entry never prints) | The guard against a second dialog is the riskiest piece; a port makes the order of history and print observable |
| DS-16 | The export carries `{printWhenReady: true, from}` in router state, read through a probe of `location.state`; Back and the after-print return follow `from`, and a direct load or a final failure leads to the Reports list (PD-5, **confirmed 2026-10-20**) | DDD-27; the origin is the one thing the address does not say |
| DS-17 | Freshness is pinned against the app's own cache settings (5-minute `staleTime`, `refetchOnMount: true`) with an answer seeded one minute old, so only `refetchOnMount: "always"` passes | A seed older than the stale time would pass without the fix |
| DS-18 | Fonts: `document.fonts.load` is asked for Quicksand (700 included) only once the report is drawn; a refused load or one finding no face is a retryable failure | DDD-28 |
| DS-19 | A return to the Reports list after printing counts one more Reports-tab opening, pinned as accepted (DDD-29); the light ground (white, full height, `color-scheme: light`, DDD-30) and the one export starter (DDD-32) are pinned by page and source scan | Accepted behaviour is pinned so a later "fix" is a deliberate change |

## Policy rows appended (`docs/architecture/atdd-infrastructure-policy.md`)

The browser's print dialog; `document.fonts.ready`; the Chromium PDF engine (real); the usage-data reporter as a
page sees it.

## Not covered in DISTILL

AC-4.5 (changesets for the touched packages): N/A, because a changeset is written with the code in DELIVER; the one
written in the first pass was removed. AC-5.5 (a later report type adds a section, not a skill): an accepted gap,
judged by review in DELIVER.

## Step reuse (Mandate 12)

N/A, because there is no Gherkin step layer in this repository: scenarios are NUnit, Vitest and Playwright tests,
and shared vocabulary lives in the test kits (`ReportPrintTestKit.tsx`, `ReportsTestKit.tsx`,
`test-support/reportAnswers.ts`, the `ReportPrintPage` page object).
