# Slice A - moved assertions

Pre-slice commit: `1ffe2be77` (code), `b17d5bec9` (documents). Slice A stopped at step 01-01 (gate r = 1.52 against 1.85, see
`slice-A-measurement.md`), so nothing moved, and this file records an empty slice.

The two checking scripts read this file by its section headings (`Moved assertions`, `Added ids`, `Accepted matcher hits`),
so keep the headings and the table's column order.

## Universe

Nothing outside this list may change in the slice.

- Test files edited: none
- Test files created: none (the spike file of the gate was deleted before the commit)
- Test helpers edited or created: none
- Production files: none
- `slowTests.ts` entries deleted or added: none

## Preserved set

Sorted test ids of the touched files before the slice (`baseline/<file>.ids.txt` and `baseline/outside-seven/`, from a run
of 2026-11-07 on `1ffe2be77`, never from source). All preserved, none moved or removed.

## Delta

| Measure | Before | After |
|---|---|---|
| Tests in the touched files (sum) | 330 (seven files) | 330 |
| `expect(` calls in the touched files (sum) | 527 | 527 |
| Summed per-test seconds | `refinementNeed` 14.57, `readiness` 12.08, `ModifyTeamSettings.test` 10.78 (single quiet run) | not run, nothing moved |
| Cut against required | | not applicable, stopped at the gate |
| Line / branch coverage of the Team directory | 96.16 / 86.72 | unchanged |
| Entries in `slowTests.ts` | unchanged | unchanged |

## Moved assertions

| Old id (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|

## Added ids

-

## Accepted matcher hits

## Checks run at close

- Slice A did not run a close step. The baseline lists were checked against a run of the same files (id lists regenerated
  from that run).
