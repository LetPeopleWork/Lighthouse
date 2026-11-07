# Slice A - moved assertions

Pre-slice commit: `1ffe2be77` (code), `b17d5bec9` (documents). Slice A stopped at step 01-01 (gate r = 1.52 against 1.85, see
`slice-A-measurement.md`). The maintainer replaced the targets afterwards (`feature-delta.md`, "The gate read 1.52, and the
target moved"), and step 01-02 moves the display and validation tests of `readiness` to a section render. Later steps add
their own rows below.

The two checking scripts read this file by its section headings (`Moved assertions`, `Added ids`, `Accepted matcher hits`),
so keep the headings and the table's column order.

## Universe

Nothing outside this list may change in the slice.

- Test files edited: `Lighthouse.Frontend/src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx` (18 of 24 tests leave, 6 wiring tests stay unmodified)
- Test files created: `Lighthouse.Frontend/src/components/Common/Team/ReadinessSettings.test.tsx` (the 18 moved tests, titles unchanged), `Lighthouse.Frontend/src/tests/renderRefinementSection.test.tsx` (the helper's four tests)
- Test helpers edited or created: `Lighthouse.Frontend/src/tests/renderRefinementSection.tsx` (new, commit `342915d95`); importers: `ReadinessSettings.test.tsx` and its own test file only
- Production files: none
- `slowTests.ts` entries deleted or added: none

## Preserved set

Sorted test ids of the touched files before the slice (`baseline/<file>.ids.txt` and `baseline/outside-seven/`, from a run
of 2026-11-07 on `1ffe2be77`, never from source), minus the 18 ids moved below. The six wiring tests of
`ModifyTeamSettings.readiness.test.tsx` keep their ids and pass unmodified: saves a changed number of Yes votes with the rest
of readiness as it was; starts a Team that has never chosen refinement at the default readiness, and saves it with no states;
keeps the chosen refinement states when readiness changes; names readiness as what keeps the settings from being saved;
switches a rule back on at the threshold it was changed to before it was switched off; saves nothing just because the form
was opened.

## Delta

| Measure | Before | After |
|---|---|---|
| Tests in the touched files (sum) | 330 (seven files) | 334 (24 readiness tests become 6 on the form and 18 on the section, plus the helper's 4) |
| `expect(` calls in the touched files (sum) | 527 | see `slice-A-measurement.md`, step 01-02 |
| Summed per-test seconds | `readiness` 11.10 (median of 5 pairs, form file) | 4.95 (form file 6 tests plus section file 18 tests) |
| Cut against required | | 55.4 % against 19.4 % required and 24 % design target; passes with margin |
| Line / branch coverage of the Team directory | 96.16 / 86.72 | measured at the slice close, A-5 |
| Entries in `slowTests.ts` | unchanged | unchanged |

## Moved assertions

Every row below is a removed line of `ModifyTeamSettings.readiness.test.tsx`. A moved test's lines are re-added in
`ReadinessSettings.test.tsx`, most of them changed to read the value the host holds (`latest()`) where the form read the
last saved value. The "saves nothing" lines of the refusal tests (`expect(saveTeamSettings).not.toHaveBeenCalled()`) have no
counterpart on the section, which saves nothing; that the form holds back a refused value stays proved on the form by
`names readiness as what keeps the settings from being saved`, and the refusals themselves by `readinessErrors.test.ts`.
For a table-driven test the removed lines are shared by its four or two ids, so some rows quote a setup line.

| Old id (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect( | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect(screen.getByRole("spinbutton", { name: VOTERS_NEEDED })).toHaveValue( | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect(screen.getByText("Send to discussion when")).toBeVisible(); | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect(screen.getByRole("checkbox", { name: NO_RULE })).toBeChecked(); | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect(screen.getByRole("spinbutton", { name: NO_THRESHOLD })).toHaveValue( | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect(screen.getByText("or more No votes")).toBeVisible(); | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect(screen.getByRole("checkbox", { name: YES_IF_RULE })).toBeChecked(); | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect( | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect(screen.getByText("or more “Yes, if…” votes")).toBeVisible(); | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them | expect(saveTeamSettings).not.toHaveBeenCalled(); | src/components/Common/Team/ReadinessSettings.test.tsx | shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings refuses fewer than one Yes vote, says why and saves nothing | expect( | src/components/Common/Team/ReadinessSettings.test.tsx | refuses fewer than one Yes vote, says why and saves nothing |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings refuses fewer than one Yes vote, says why and saves nothing | expect(saveTeamSettings).not.toHaveBeenCalled(); | src/components/Common/Team/ReadinessSettings.test.tsx | refuses fewer than one Yes vote, says why and saves nothing |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings refuses fewer voters than Yes votes, says why and saves nothing | expect( | src/components/Common/Team/ReadinessSettings.test.tsx | refuses fewer voters than Yes votes, says why and saves nothing |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings refuses fewer voters than Yes votes, says why and saves nothing | expect(saveTeamSettings).not.toHaveBeenCalled(); | src/components/Common/Team/ReadinessSettings.test.tsx | refuses fewer voters than Yes votes, says why and saves nothing |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches off the No rule on its own, keeping the “Yes, if…” rule | expect(theReadinessLastSaved()).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | switches off the No rule on its own, keeping the “Yes, if…” rule |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings saves a changed number of “Yes, if…” votes that send to discussion | expect(theReadinessLastSaved()).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | saves a changed number of “Yes, if…” votes that send to discussion |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule back on at the threshold it had before | expect(theReadinessLastSaved()?.discussWhen).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | switches a rule back on at the threshold it had before |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule back on at the threshold it had before | expect(theReadinessLastSaved()).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | switches a rule back on at the threshold it had before |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches on a rule stored as off at its default threshold | expect(theReadinessLastSaved()).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | switches on a rule stored as off at its default threshold |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings refuses '' in No votes that send to discussion, says why and saves nothing | expect( | src/components/Common/Team/ReadinessSettings.test.tsx | refuses '' in No votes that send to discussion, says why and saves nothing |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings refuses '' in “Yes, if…” votes that send to discussion, says why and saves nothing | expect(saveTeamSettings).not.toHaveBeenCalled(); | src/components/Common/Team/ReadinessSettings.test.tsx | refuses '' in “Yes, if…” votes that send to discussion, says why and saves nothing |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings refuses '0' in No votes that send to discussion, says why and saves nothing | it.each([ | src/components/Common/Team/ReadinessSettings.test.tsx | refuses '0' in No votes that send to discussion, says why and saves nothing |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings refuses '0' in “Yes, if…” votes that send to discussion, says why and saves nothing | { field: NO_THRESHOLD, typed: "0" }, | src/components/Common/Team/ReadinessSettings.test.tsx | refuses '0' in “Yes, if…” votes that send to discussion, says why and saves nothing |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings marks Voters needed as refused and leaves Yes votes needed unmarked | expect( | src/components/Common/Team/ReadinessSettings.test.tsx | marks Voters needed as refused and leaves Yes votes needed unmarked |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings marks Yes votes needed as refused and leaves Voters needed unmarked | expect( | src/components/Common/Team/ReadinessSettings.test.tsx | marks Yes votes needed as refused and leaves Voters needed unmarked |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings marks No votes that send to discussion as refused, describes it by why, and leaves “Yes, if…” votes that send to discussion unmarked | expect(refusedField).toHaveAttribute("aria-invalid", "true"), | src/components/Common/Team/ReadinessSettings.test.tsx | marks No votes that send to discussion as refused, describes it by why, and leaves “Yes, if…” votes that send to discussion unmarked |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings marks No votes that send to discussion as refused, describes it by why, and leaves “Yes, if…” votes that send to discussion unmarked | expect(acceptedField).toHaveAttribute("aria-invalid", "false"); | src/components/Common/Team/ReadinessSettings.test.tsx | marks No votes that send to discussion as refused, describes it by why, and leaves “Yes, if…” votes that send to discussion unmarked |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings marks “Yes, if…” votes that send to discussion as refused, describes it by why, and leaves No votes that send to discussion unmarked | expect(refusedField).toHaveAccessibleDescription(DISCUSSION_RULE_ERROR); | src/components/Common/Team/ReadinessSettings.test.tsx | marks “Yes, if…” votes that send to discussion as refused, describes it by why, and leaves No votes that send to discussion unmarked |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings marks “Yes, if…” votes that send to discussion as refused, describes it by why, and leaves No votes that send to discussion unmarked | expect(acceptedField).not.toHaveAttribute("aria-describedby"); | src/components/Common/Team/ReadinessSettings.test.tsx | marks “Yes, if…” votes that send to discussion as refused, describes it by why, and leaves No votes that send to discussion unmarked |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings does not let any of its numbers step below one vote | expect(field).toHaveAttribute("min", "1"); | src/components/Common/Team/ReadinessSettings.test.tsx | does not let any of its numbers step below one vote |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings does not let any of its numbers step below one vote | expect(field).toHaveAttribute("step", "1"); | src/components/Common/Team/ReadinessSettings.test.tsx | does not let any of its numbers step below one vote |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold | expect(theReadinessLastSaved()?.discussWhen).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | switches a rule that was refused when switched off back on at its last allowed threshold |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold | expect(theReadinessLastSaved()?.discussWhen).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | switches a rule that was refused when switched off back on at its last allowed threshold |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold | expect(theReadinessLastSaved()?.discussWhen).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | switches a rule that was refused when switched off back on at its last allowed threshold |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold | expect(theReadinessLastSaved()?.discussWhen).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | switches a rule that was refused when switched off back on at its last allowed threshold |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx :: Readiness in the Refinement section of a Team's settings switches on a No rule stored as off at its default threshold | expect(theReadinessLastSaved()?.discussWhen).toEqual({ | src/components/Common/Team/ReadinessSettings.test.tsx | switches on a No rule stored as off at its default threshold |

## Added ids

- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings does not let any of its numbers step below one vote
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings marks No votes that send to discussion as refused, describes it by why, and leaves “Yes, if…” votes that send to discussion unmarked
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings marks Voters needed as refused and leaves Yes votes needed unmarked
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings marks “Yes, if…” votes that send to discussion as refused, describes it by why, and leaves No votes that send to discussion unmarked
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings marks Yes votes needed as refused and leaves Voters needed unmarked
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings refuses '0' in No votes that send to discussion, says why and saves nothing
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings refuses '0' in “Yes, if…” votes that send to discussion, says why and saves nothing
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings refuses fewer than one Yes vote, says why and saves nothing
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings refuses fewer voters than Yes votes, says why and saves nothing
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings refuses '' in No votes that send to discussion, says why and saves nothing
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings refuses '' in “Yes, if…” votes that send to discussion, says why and saves nothing
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings saves a changed number of “Yes, if…” votes that send to discussion
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule back on at the threshold it had before
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings switches a rule that was refused when switched off back on at its last allowed threshold
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings switches off the No rule on its own, keeping the “Yes, if…” rule
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings switches on a No rule stored as off at its default threshold
- src/components/Common/Team/ReadinessSettings.test.tsx :: Readiness in the Refinement section of a Team's settings switches on a rule stored as off at its default threshold
- src/tests/renderRefinementSection.test.tsx :: The Refinement section render shows the value the Team was stored with until the admin edits it
- src/tests/renderRefinementSection.test.tsx :: The Refinement section render shows a change the admin makes as its new value
- src/tests/renderRefinementSection.test.tsx :: The Refinement section render builds a second edit on the first edit's result, as the form does
- src/tests/renderRefinementSection.test.tsx :: The Refinement section render lets a test read the value the admin has now

## Accepted matcher hits

## Checks run at close

- Step 01-02 only; the close checks (A-5) run after the last step.
