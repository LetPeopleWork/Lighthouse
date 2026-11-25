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

## Suspected cause (to be confirmed by the oracle's RED)

`ItemListManager` keeps the last suggestion that MUI Autocomplete reported through `onHighlightChange` and adds it
on Enter. MUI reports only highlight changes that come from the mouse, keyboard or touch. It does not report the
reset when the option list changes after a paste or a keystroke, so the stored suggestion goes stale and Enter adds
it instead of the typed text.
