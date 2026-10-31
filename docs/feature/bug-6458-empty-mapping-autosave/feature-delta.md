# Bug 6458 (b): an unfinished state mapping is never saved

ADO Bug #6458, part (b). Part (a), the category lists that cannot tell a raw state from a mapping name, is
`docs/feature/bug-6458-typed-category-entries/` and goes through DESIGN next.

## Wave: DELIVER / [REF] Maintainer decisions

- Root cause B confirmed; (b) fixed now, (a) to DESIGN (2026-10-31). RCA:
  `docs/feature/bug-6458-typed-category-entries/rca.md`, section 3 Branch B and 6.1.
- Incomplete rows are left out of the save rather than pausing autosave for them (B1 over the pause
  variant). Consequence, kept on purpose: blanking the name of a saved mapping saves the settings without
  it, its states return to Doing as raw states, and the row stays in the editor.
- Sketch approved: the duplicate-name and shared-source-state errors of complete mappings join the
  existing "Your changes are not being saved" list in the editor's wording. Field errors never appear there.
- Showing the server's 400 message in the save indicator (B3) is out.
- Rigor profile `standard`: crafter Sonnet, reviewer Haiku. Run in AFK mode, push authorised.

## Wave: DELIVER / [REF] Implementation summary

`useModifySettings` drops mappings with a blank name or no states from every payload (debounced autosave,
leave-page flush, manual save, validation) and skips an autosave whose payload matches what was last
loaded or saved, so an Add mapping click no longer sends a save or triggers a tracker refresh.
`mappingAutoSaveBlockers` runs the editor's own duplicate-name, shared-state and name-clash rules over the
complete mappings; Team and Portfolio settings use it as their mapping blocker. The state options in the
Doing/To Do/Done lists, cycle-time boundaries and wait states offer only complete mappings.

## Wave: DELIVER / [REF] Commits

- `bfd39ec97` fix(state-mapping): do not save a mapping that is still being filled in
- `be3adace6` fix(state-mapping): pause autosave for mappings the server would reject
- `d969b7ce0` fix(settings): do not autosave a payload that has not changed
- `bf58ca8ba` fix(state-mapping): offer only complete mappings as state options
- `18f5c26eb` test(state-mapping): assert the saved mappings and name the blocked-save test truthfully
- `6e493d0a5` test(settings): kill-round tests for the remembered saved form

## Wave: DELIVER / [REF] Quality gates

- DES integrity: 2/2 steps with complete traces.
- Refactor (L1–L6): no changes needed.
- Adversarial review (Haiku): NEEDS_REVISION, 1 high / 5 medium / 2 low. Fixed M2 (unchanged payload
  saved), M3 (name-only mappings offered as options), M4 (page tests did not assert the payload), M5
  (misnamed test). Kept H1 as the maintainer's B1 choice (above). Deferred M1 to part (a): the frontend
  folds case with `toLowerCase` and trims with JS `trim`, while the backend uses `OrdinalIgnoreCase` and
  `IsNullOrWhiteSpace`, so exotic characters (`ı`/`I`, `ſ`/`s`, U+0085) can still reach a 400; the fold
  lives in `normalise`, shared with the reconciliation that part (a) replaces. L1 (an edit inside the
  debounce window is dropped when the next edit blocks) is pre-existing; L2 is outside this change.
- Mutation (frontend): 88.66 %, see `mutation/results.md`. Backend N/A.

## Wave: DELIVER / [WHY] Upstream Issues

- None for (b). Follow-up for (a): the Unicode fold and whitespace parity above.
