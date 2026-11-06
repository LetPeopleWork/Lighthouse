# Slice X - moved assertions (copy to `slice-X-moved-assertions.md` before the first rewrite commit)

Pre-slice commit: `<sha of HEAD when the slice was declared>`. The close-time alternating pairs check this out for the "before" side.

## Universe

Nothing outside this list may change in the slice.

- Test files edited: `<named>`
- Test files created: `<named>`
- Test helpers edited or created: `<named>`
- Production files: `<none>` or `<named extraction, its own refactor commit, no test change in it>`
- `slowTests.ts` entries deleted or added: `<none expected>`

## Preserved set

Sorted test ids of the touched files before the slice (`baseline/<file>.ids.txt`, from a run, never from source), minus the
ids listed below as moved or removed. Every preserved id keeps its `file :: name` and passes unmodified.

## Delta

| Measure | Before | After |
|---|---|---|
| Tests in the touched files (sum) | | |
| `expect(` calls in the touched files (sum, `baseline/<file>.expects.txt`) | | |
| Summed per-test seconds, 5 alternating pairs (load average and user+sys CPU seconds recorded per pair) | | |
| Line / branch coverage of the touched directory | | |
| Entries in `slowTests.ts` | | |

## Moved assertions

Every `expect(` removed or shrunk in a touched file has a row. "Assertion text" is the removed `expect(...)` line verbatim.
A moved test that keeps its title and assertions still gets a row (its file changed).

| Old test (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
| | | | |

## Checks run at close

- [ ] `git diff -U0 <before> -- <file> | grep -c '^-.*expect('` per touched file equals the rows for that file
- [ ] `bash docs/feature/story-6561-t13-slowest-frontend-test-files/baseline/check-behaviour-free.sh <before>` exits 0
- [ ] After-run ids diffed against the baseline: every missing id is a row above, every added id is listed
- [ ] Each new call site or wiring proved by a deliberate break (what was broken, which test failed), recorded below
- [ ] Coverage of the touched directory, line and branch, not lower
