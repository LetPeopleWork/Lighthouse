# Slice X - moved assertions (copy to `slice-X-moved-assertions.md` before the first rewrite commit)

Pre-slice commit: `<sha of HEAD when the slice was declared>`. The close-time alternating pairs check this out for the "before" side.

The two checking scripts read this file by its section headings (`Moved assertions`, `Added ids`, `Accepted matcher hits`),
so keep the headings and the table's column order.

## Universe

Nothing outside this list may change in the slice.

- Test files edited: `<named>`
- Test files created: `<named>`
- Test helpers edited or created: `<named>`, with every importer of each helper named
- Production files: `<none>` or `<named extraction, its own refactor commit, no test change in it>`
- `slowTests.ts` entries deleted or added: `<none expected>`

## Preserved set

Sorted test ids of the touched files before the slice (`baseline/<file>.ids.txt` and `baseline/outside-seven/`, from a run,
never from source), minus the ids listed below as moved or removed. Every preserved id keeps its `file :: name` and passes
unmodified. The id lists pin **names**, not assertions: an assertion lost inside a test that keeps its name is caught only by
the removed-`expect(` rows below.

## Delta

| Measure | Before | After |
|---|---|---|
| Tests in the touched files (sum) | | |
| `expect(` calls in the touched files (sum, `baseline/<file>.expects.txt`) | | |
| Summed per-test seconds, valid pairs of 5 alternating pairs, at least 3 valid (load average sampled at the start and end of each run, user+sys CPU seconds recorded per pair) | | |
| Line / branch coverage of the touched directory | | |
| Entries in `slowTests.ts` | | |

## Moved assertions

Every `expect(` removed or shrunk in a touched file has a row. "Assertion text" is the removed `expect(...)` line verbatim; the
script counts every `expect(` in the cell and compares it per file with the removed lines of the diff. A moved test that keeps
its title and assertions still gets a row (its file changed). The first cell is the **full id**, `file :: test`, as in the
baseline list; for a helper file, `file :: <anything>`.

| Old id (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
| | | | |

## Added ids

One full id (`file :: test`) per line, for every id in the after-run that is not in the baseline: a moved test under its new
file, a merged test, a new direct test.

-

## Accepted matcher hits

One line per added line the script flagged that is not a weaker assertion (for instance a `toContain(` on a string that was
already one), pasted as the script printed it, each followed by one clause of reason. Empty when nothing was flagged.

## Checks run at close

- [ ] `bash docs/feature/story-6561-t13-slowest-frontend-test-files/baseline/check-behaviour-free.sh <before> <this file>` exits 0
      (add `--allow-extraction` only when the maintainer said yes to Q7)
- [ ] `bash docs/feature/story-6561-t13-slowest-frontend-test-files/baseline/run-touched-files.sh <before> <this file>`
      ends with `OK` (it runs every baseline, changed and new test file and diffs the id union)
- [ ] Each new call site or wiring proved by a deliberate break (what was broken, which test failed), recorded below
- [ ] Coverage of the touched directory, line and branch, not lower
