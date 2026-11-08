---
name: test-writing
description: How Lighthouse writes tests so they are fast, sit in the lowest layer that can prove them, and can fail. Load it before you write, change or plan any test, acceptance scenario text or test plan, then read the half for your stack (references/frontend.md for the frontend). Applies to DISTILL and DELIVER steps and to reviewers of their output.
---

# Test writing

Read this file in full, then the half for your stack: `references/frontend.md` for the frontend. planned: the backend half.
Read each file with the read tool in full mode and fresh, not as a line window. Each file ends with a line "End of <file>". Quote that last line back in
your report, one per file. If you did not see it, you read a cut copy: read the file again in pages.

## Checklist

Tick every item before you write a test, and again before you hand it over.

1. Name the behaviour in one sentence, and the lowest layer that can prove it (see "Layer rule").
2. A rule (arithmetic, ordering, filtering, validation, wording, a decision) is asserted on a function or hook, not through a render.
3. One render per wiring: several checks of one wiring share one render.
4. The fixture is as small as the behaviour allows. A long list only when the list itself is the test.
5. Time is a fake clock, advanced by the test or left to follow real ticks (`shouldAdvanceTime`). Never a sleep or a real wait.
6. A slow or flaky test is made smaller or deterministic. It is never given a longer timeout or a retry.
7. The test passes alone, in the file's order and in a shuffled order. It builds its own state and resets what it changes.
8. A new view pins its loading look and its failure look, not only the loaded one.
9. The test can fail: you deleted or inverted the production line it claims to cover, saw it go red, put the line back, and `git diff` shows that file as it was.
10. Names are plain words. The test says Feature and Work Item, never a tracker's word. No nWave scenario tags (see the last section).
11. You ran the fast loop for what you touched (`pnpm test:fast --changed` for logic tests, `pnpm test --changed` after a component edit) and the test the step names.

planned: the short text of the fast cycle rule.

## Layer rule

A test lands in the lowest layer that can prove it. From the bottom: a function or hook with no DOM (a `.test.ts` file); one rendered
component with its wiring (a `.test.tsx` file); an integration test across the host; an end-to-end test with Playwright. Moving a test
down beats adding one higher up. Time is the gate: each test has a per-test limit, and a test that cannot meet it belongs lower.

The frontend limit is read from `vitest.config.ts` (`BUDGET_SECONDS` for a `.test.tsx` test, `FAST_BUDGET_SECONDS` for any other test
file) by the reporter `src/tests/timeBudget/timeBudgetReporter.ts`, which prints a `Time budget:` summary line ending in `offenders`.
A test over its limit that is not in the allow-list `src/tests/timeBudget/slowTests.ts` fails the run. The allow-list says a test is "listed here with why it
is slow": a listed test carries a reason and a ceiling, and is the exception, not a way out.

Finding: tests asserted on a function took a fraction of the time of tests proving the same kind of rule through a rendered page or dialog,
and the slowest tests in a green main run are page and dialog tests that render long lists.

## A check that cannot fail is not a check

Finding: a test that compares a value with the result of the same computation, or asserts on a mock set up to return the value it
asserts, stays green after the production line is deleted. Stryker mutates product code per feature, but StrykerJS does not mutate JSX
text, and a change that only adds or edits tests has nothing to mutate. So the deliberate break is the proof, not an option: delete or
invert the production line the test covers, run the one test, see it red for the reason in its name, put the line back.

Example, with `getBubbleSize` from `src/utils/charts/scatterMarkerUtils.tsx`:

```ts
// Cannot fail: the expectation is computed by the code under test.
expect(getBubbleSize(4)).toBe(getBubbleSize(4));

// Can fail: the expected value is written out.
expect(getBubbleSize(4)).toBe(11);
```

## The pyramid is a direction

Steer by this picture: about ninety percent of tests fast, about nine and a half percent integration, about half a percent end to end,
by test count; a target picture, not a measurement. It is a direction and gates nothing, because a share can be met by adding trivial tests. What gates is time, by the layer
rule above.

## No nWave scenario tags

Write no nWave scenario tags in any test, test title, comment or scenario text: a tag is a label that no tool here reads. Keep any such
classification in the feature workspace, not in the code. The Playwright title tags that the package scripts filter on (`@screenshot`,
`@auth`, `@rbac`, `@proxyauth`) are real and stay.

End of SKILL.md
