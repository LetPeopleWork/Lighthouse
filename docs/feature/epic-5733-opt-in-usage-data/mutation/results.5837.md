# Mutation testing — Epic 5733 slice 04 (ADO #5837), 2026-09-13

Gate: 80 %. **Backend 90.91 %, frontend 99.20 %.** Both runs scoped to the code this slice changed;
the two stacks were run sequentially, because an overlapping run once produced a result that was
100 % `Timeout` and therefore no result at all.

| Stack | Tested | Killed | Survived | Score |
|---|---|---|---|---|
| Backend | 66 | 60 | 6 | **90.91 %** |
| Frontend | 125 | 124 | 1 | **99.20 %** |

The configs beside this file are the reproducible record: `stryker.5837.backend.json`,
`stryker.5837.frontend.json`, `vitest.stryker.5837.ts`. The raw JSON reports are deliberately not
kept — they are re-derivable from those configs, they embed a copy of every mutated file's source
and so go stale the moment that code changes, and Stryker.NET's writes an entry for every file in
the project rather than the three that were mutated, which came to 12 MB. What is worth keeping is
the judgement below, not the machine's output.

## What it found that two adversarial reviewers did not

Four real gaps, all in this slice's own work, all with a green suite over them.

**The threshold had no test for its own mechanism.** A tab counts once somebody has stayed on it
five seconds, and that only works because leaving the page calls off the count the page started.
Nothing exercised the cancel: every case mounted on a tab and stayed there, so none of them ever
changed page. With the cancel removed, the clock a tab started goes on running after somebody has
moved elsewhere — clicking through three tabs to find something records all three, which is the
exact behaviour the threshold was added to prevent. The feature was hours old and its central
mechanism was untested.

**The shape check was asserted in one direction only.** A Portfolio opening carrying a Team's
address was refused; the mirror was never asked. Blanking the Team entry — which makes its check
accept any address at all — left the suite green.

**The refactor opened a hole.** A tab belonging to the other kind of page has no name on this one's
list. Reading the page through the helper that asks only for the page hid a half-built opening: one
naming the kind of page and carrying no page at all. The server refuses it, but only after it has
left the browser.

**The digit guard's anchors were free.** It exists so `/teams/new` is not counted as a Team, and it
requires digits all through — but no address in the suite both contained a digit and was not one, so
every mutant relaxing it to "contains a digit" survived.

Each fix was verified by applying the mutant by hand and watching the new case fail.

## Survivors, and why none of them earns a test

**Frontend, one — and it is a phantom.** `usageDataRouteKeys.ts:67`, the digit guard replaced by
`true`. Applied by hand it is killed four times over, by tests Stryker itself listed as having run.
This repository has a documented history of StrykerJS reporting false survivors, which is why every
survivor here was hand-checked before anything was written for it. A frontend score here is a lower
bound.

Also on that file, a `RuntimeError` rather than a survivor: `FLUSH_INTERVAL_MS` mutated from
`30 * 1000` to `30 / 1000` gives a timer firing every thirty microseconds, which is an exhausted
runner rather than a missing test.

**Backend, five.** Four are on the collector's project-key fallback in `PostHogUsageDataPublisher` —
no scenario sets a key of its own, so the branch choosing between a configured key and the one the
product ships with is never taken both ways. That is a real gap and it is **not this slice's**: the
code belongs to the slice that built the pipe, and it matters to a fork collecting into its own
project. Recorded here rather than papered over, because the next person to touch that file should
know.

The fifth is `ArgumentNullException.ThrowIfNull(services)` in a dependency-injection extension
method. Nothing calls it with null and nothing sensibly could. Equivalent.
