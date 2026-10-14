# ADR-222: Report windows are inclusive calendar days in the instance zone; the Now length is stored on the report

- **Status**: Accepted (2026-10-13, after the Final Wave Review; proposed in DESIGN 2026-10-03)
- **Date**: 2026-10-03
- **Feature**: epic-5878-baseline (slices 01, 02, 07, 10)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D37: Then = a picked end date and a length going back, ≥ 14 days, the whole window inside `DoneItemsCutoffDays`,
calendar days, **inclusive at both ends**, in the instance's time zone ("90 days ending 31 Jul 2026 = 3 May – 31 Jul").
D38: Now is the full rolling window of the saved length ending today, may overlap Then. D43: the Now length is saved
on the report, editable only by editors, not in the page address — because scheduled sending (5882) needs a defined
window. D21: a refused Then names the earliest allowed start.

What exists:

- `ILighthouseClock.Today` is the instance's calendar day (Bug #5567 seam); `ToInstanceDay` reduces instants.
- `BaselineValidationService.Validate` checks the PBC Baseline: it measures length as `end − start` (so an
  **inclusive** 14-day window measures 13 and is refused), and every message says "Baseline" (C3).
- The frontend's `dateWindow.ts` computes Metrics windows in the **viewer's** zone (Bug #5566) and is URL-driven.

## Decision

1. **Windows are computed on the server** from `ILighthouseClock.Today`: Then = `[end − (L − 1), end]` from the
   picked end day and length; Now = `[today − (L − 1), today]` on every read. The wire carries `DateOnly` days
   (`yyyy-MM-dd`), never instants, in and out. The frontend never reads the viewer's clock for a report; the only day
   arithmetic it does is on days the server sent (decision 5).
2. **A pure `ReportWindowPolicy`** validates both: length ≥ 14 inclusive days; end ≤ today; start ≥
   `today − DoneItemsCutoffDays` (a cutoff of 0 means no limit). A refusal is a closed reason code with the earliest
   allowed start, and the frontend writes the copy (no "Baseline"). It is checked for Then at creation, and for Now at
   creation and on edit. The policy reads nothing: `today` and the cutoff days are arguments, read by the query or
   command that calls it (from `ILighthouseClock` and the read-only `IReportOwnerLookup` port) — so its tests need no fakes.
3. **The Now length is a template-specific member of the Then & Now payload**, written at creation (default = Then
   length) and changed only through the edit endpoint under Write. No query parameter on the read can change it.
4. **Day arithmetic happens once.** Day counts shown in the header ("63 days since Then ended") are computed on the
   server as `today − thenEnd` in days and sent as a number.
5. **The wizard shows a refusal before Create from bounds the server sends** (2026-10-13). The Create / Edit wizard
   (DT3) must show the resolved window and any refusal under the field while the user types, before anything is
   sent. `GET …/reports/templates` returns `today`, `earliestStart` and `minimumLengthDays`, all produced by
   `ReportWindowPolicy` for this owner. The wizard compares the entered window against them — end ≤ `today`, start ≥
   `earliestStart`, length ≥ `minimumLengthDays` — and shows start = end − (L − 1) computed on plain calendar days
   from those strings. Create and Save re-validate on the server, which stays the only authority: a server refusal
   is shown with the same copy by its code, so a client slip can delay a refusal, never store a bad window. The
   rule's **values** (14, the cutoff, today) come from one source, the server; the **comparison logic** has two
   implementations — the policy and the wizard's pre-check, including its shown start — kept together by one shared
   boundary table (Enforcement). Every copy that names a value interpolates the server's (`minimumLengthDays`,
   `earliestStart`); the wizard's one-click presets (Team 30 / 90, Portfolio 90 / 180) are UI shortcuts, a product
   choice of which lengths to offer, not rule values. **`earliestStart: null`** — an owner with a cutoff of 0 —
   means no lower bound: the start comparison is skipped and no cutoff copy is shown. The templates read is fetched
   afresh each time the wizard opens, so `today` cannot outlive midnight in a tab left open.

## Alternatives considered

- **Reuse `BaselineValidationService`.** Off by one under inclusive counting, worded for the PBC Baseline, and its
  contract belongs to a setting the report must stay independent of. Changing it would move the PBC's behaviour.
  **Rejected.**
- **Compute windows in the frontend** with `dateWindow.ts` and send start/end. The viewer's zone is not the
  instance's; two viewers would see different Now windows and a server-side render (5882) would see a third.
  **Rejected.**
- **Now length per view in the URL** (the earlier D22). Superseded by the maintainer (D43). **Rejected.**
- **A window-check endpoint the wizard calls as the user types** (one source for the comparisons as well as the
  values). A round-trip per keystroke, Next waiting on the network, and a loading and a failure look inside each
  wizard step — on a slow connection the refusal would lag the field it explains. **Rejected** in favour of
  decision 5 plus a shared boundary table.
- **The wizard carries its own copy of 14 and of the cutoff rule.** Two sources of the rule's values; a cutoff
  change on the server would leave the wizard offering windows the server refuses. **Rejected.**

## Consequences

- **Positive**: every reader, and later every scheduled send, sees the same Now window for the same day.
- **Positive**: inclusive counting matches D37's worked examples exactly; the Metrics endpoints are called with the
  same start and end days, so AC-2.3's parity check compares like with like.
- **Negative**: a viewer west or east of the instance's zone may see "today" differ from their own calendar near
  midnight. Accepted: the instance zone is the product's rule for calendar days (Bug #5567).
- **Negative**: if `DoneItemsCutoffDays` later shrinks below a saved Now length, Now still covers its full length;
  days before the cutoff simply hold no finished items. Not re-validated on read.

## Enforcement

- NUnit: D37's two worked examples as table tests; 14 inclusive days accepted, 13 refused; earliest start = today −
  cutoff; cutoff 0 accepts any start.
- NUnit with a non-UTC instance zone (the backend suite is pinned off UTC since Bug #5567): an end date picked as a
  day never shifts.
- The read endpoint ignores any window parameter (contract test).
- One boundary table (13 / 14 days; end = today / today + 1; start = earliest / earliest − 1; for Then and Now)
  runs against `ReportWindowPolicy` in NUnit and against the wizard's pre-check in Vitest, with the same rows and the
  same expected codes, so the two cannot drift unnoticed. It includes a cutoff-0 row (`earliestStart` null, an old
  start, no refusal).
- Vitest: with bounds whose `minimumLengthDays` and `earliestStart` differ from the shared table's, the refusal copy
  names those values — a literal 14 or a fixed date in the copy fails.
- ArchUnitNET: `ReportWindowPolicy` depends on neither `ILighthouseClock` nor any repository or port (part of E15, which holds the cap policy ~~and the beyond-limits rule~~ to the same; *the beyond-limits rule was dropped by D49, 2026-10-14*).

Cross-refs: ADR-219, ADR-221; Bug #5567 conventions in the brief's `fix-backend-utc-today-anchor` section.
