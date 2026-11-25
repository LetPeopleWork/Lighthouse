# Bug 6700 — several charts list the same Work Item many times

ADO: Bug #6700 (tagged Release Notes).

## Maintainer decisions (2026-11-25)

- **Mode:** AFK. Each slice wraps up with refactor and review; mutation runs once in the final gates. The slice is
  pushed on its own and CI is watched.
- **Rigor profile:** `standard` for S1: acceptance designer and crafter on Sonnet, reviewers and EXAMINE on Haiku.
- **Side note in the report** ("nothing is shown under Total Work Item Age Over Time"): out of scope here. It is
  noted at the hold, so the maintainer can raise a separate Bug.
- **UI sketch:** not needed. No layout, control, column or copy changes. Only the rows of four existing data lists
  change: each Work Item is listed once instead of once per day.
- **Loading and failure looks:** N/A. The fix fetches no new data.
- **EXAMINE:** run it. If the examiner again can record only observation gaps (as for Bug 6698), abort the draft,
  record the waiver here, and the maintainer looks at the real app after the push.

## Likely cause (to be confirmed by the oracle's RED)

The four over-time charts (Work Items In Progress Over Time, Total Work Item Age Over Time, and the Work In Progress
and Total Work Item Age Process Behaviour Charts) build their data list from the run-chart payload, which holds the
Work Items in progress **per day**. `extractWorkItems` flattens all days into one list without removing repeats, so a
Work Item in progress for 20 days of the range is listed 20 times. The Jira links and parents play no part.
