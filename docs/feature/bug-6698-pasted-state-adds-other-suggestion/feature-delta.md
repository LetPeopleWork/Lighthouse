# Bug 6698 — pasting a state adds a different, earlier suggested state

ADO: Bug #6698 (Custom.ReportedBy set; tagged Release Notes).

## Maintainer decisions (2026-11-25)

- **Rigor profile:** `standard` for S1: acceptance designer and crafter on Sonnet, reviewers and EXAMINE on Haiku.
- **Release Notes:** tagged. The reporter is credited as a contributor at the next release.
- **UI sketch:** not needed. No layout, control, column or copy changes. Only the value that Enter adds changes,
  so the field adds what the user pasted or typed. The fix is in the shared `ItemListManager`, so every list built
  on it gets it: the To Do, Doing and Done states, work item types, wait states, refinement tags, feature size
  states, the estimation field and the new-item forecaster.
- **Loading and failure looks:** N/A. The fix fetches no new data, and the suggestion fetch and its spinner do not
  change.

- **P1 restated (my own wording, not a scope change):** the first wording ("the value added is exactly what was
  entered") forbade two behaviours users rely on today: picking a suggestion with the arrow keys or the mouse, and a
  typed value taking a matching suggestion's casing. The defect is only the stale highlight.
- **Review round 2, option B (maintainer):** pin the empty-field case as a guard AND fix the "suggestions change while
  the list is open" case (F3), rather than accepting it as residual.
- **EXAMINE waived for this bug, option B (maintainer):** the examiner's only admitted probe (`frontend-tests`)
  shows passing test names, which it rightly treats as producer evidence; it cannot observe a paste and Enter, so it
  recorded observation gaps for E2–E4, and `nwdes examine close` accepts only `accepted`. The draft was aborted. The
  real-app check is the maintainer's own look after the push. Follow-up: a Continuous Improvement item for a public
  frontend EXAMINE probe (the same gap as Story 6648).

## Cause (confirmed by the oracle's RED on a0bf0f807)

`ItemListManager` kept the last suggestion that MUI Autocomplete reported through `onHighlightChange` and added it
on Enter. MUI reports only highlight changes that come from the mouse, keyboard or touch. It does not report the
reset when the option list changes after a paste or a keystroke, so the stored suggestion went stale and Enter added
it instead of the typed text. The likely everyday trigger: the list opens under a resting mouse pointer, which
highlights an option without the user noticing; a paste and Enter then added that option.

## Fix, in three review rounds

1. `32d978b45`: drop the stored highlight when the user edits the text.
2. `dfd3a6969` (review round 1): also drop it when the list closes (Escape, leaving the field).
3. `05e96ccc2` (review round 2): on Enter, use the stored highlight only while the list still offers it.

## Residual risk (accepted, non-blocking review finding)

If, while the list stays open, the suggestions change so the highlighted option disappears and then change again so
it comes back, Enter could add it although MUI no longer highlights it. Reasoned from MUI's source by the reviewer,
not reproduced. Nothing in Lighthouse changes a list's suggestions twice while it is open (they load once).
