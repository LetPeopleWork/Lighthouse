# Mutation testing — 6054 (a finished or in-progress Feature can show a start date in the future)

Run 2026-09-21 against `main` @ `15e8cf94c`, with the tree frozen after the last code commit. Gate is
80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **90.38 %** | 156 | 141 | 11 | 4 | 0 | 3 m 15 s |
| Frontend (StrykerJS) | **98.48 %** | 66 | 65 | 1 | 0 | 0 | 30 s |

Configs: `stryker.6054.backend.json`, `stryker.6054.frontend.json`, `vitest.stryker.6054.config.ts`.

**The headline number is not the interesting one.** Both stacks pass comfortably, but the backend score
is diluted by whole-file mutation of two large files whose changed regions are small. The number that
answers "would the tests have caught this bug" is below: **every mutant inside the two changed methods
was killed.**

## Backend

Scope sanity check: `19397 mutants created` across the whole backend, `19245` skipped by the mutate
filter, **`152` tested** — consistent with two files and no more.

| file | tested | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- |
| `Models/Feature.cs` | 83 | 72 | 9 | 2 | 86.75 % |
| `Services/Implementation/WriteBackTriggerService.cs` | 73 | 69 | 2 | 2 | 94.52 % |

### The changed methods: 0 survivors

Stryker.NET **ignores line ranges** — a `mutate` entry always widens to the whole file — so both files
were mutated entire, and the survivors are distributed across code this bug never touched.

- `Feature.WhenWorkBegins` spans `Feature.cs:126-146`. Survivors and no-coverage mutants sit at lines
  38, 78, 101, 155 (×2), 211, 265, 271, 308, 314, 320. **None inside 126-146.**
- `ResolveStartValue` spans `WriteBackTriggerService.cs:303-338`. Survivors and no-coverage mutants sit
  at lines 43, 188, 189, 359. **None inside 303-338.**

Every mutation of the code this fix changed was killed by a test this fix wrote. That is the claim the
gate exists to support, and it is stronger than the file-level percentage suggests.

### Accepted survivors — pre-existing, outside the change

All 15 backend survivors and no-coverage mutants are in untouched regions of the two files. They are
pre-existing gaps in code this bug did not modify, surfaced only because .NET Stryker cannot mutate a
line range. Fixing them means writing tests for unrelated behaviour, which belongs to whatever change
next touches that code, not to a bug fix whose diff is six insertions and ten deletions.

Representative examples, to show the shape rather than to enumerate all fifteen: a null-coalescing
mutation at `Feature.cs:155` in the per-team forecast lookup; a `FirstOrDefault()` to `First()` mutation
at `:265`; two log-message string mutations at `WriteBackTriggerService.cs:188-189`.

Recorded rather than fixed. The full list is in
`StrykerOutput/2026-09-21.11-36-12/reports/mutation-report.json`.

### Not mutated

Nothing was excluded. Both changed production files were mutated whole, which is the only granularity
Stryker.NET offers.

## Frontend

One file mutated: `deliveryTimelineModel.ts`. 66 mutants, 65 killed.

### Accepted survivor — equivalent, provably

`deliveryTimelineModel.ts:128:50`, `ArrayDeclaration`:

```
-   observedStart ?? dateAt(start?.percentiles ?? [], percentile);
+   observedStart ?? dateAt(start?.percentiles ?? ["Stryker was here"], percentile);
```

This cannot be killed, and not for want of a test. `dateAt` is
`percentiles.find((forecast) => forecast.probability === probability)?.expectedDate`. Against `[]`,
`find` returns `undefined`. Against `["Stryker was here"]`, `find` evaluates
`"Stryker was here".probability`, which is `undefined` and never equal to a percentile, so it also
returns `undefined`. Both arms produce `undefined` and no observable behaviour differs.

A genuinely equivalent mutant. Do not re-litigate it on a later run.

## Notes for the next run

- The backend config's `test-case-filter` names every test class covering both mutated files, not just
  the headline ones. A file whose tests are filtered out reports survivors across the board, and that
  is indistinguishable in the report from a real coverage gap.
- `vitest.stryker.6054.config.ts` must sit in `Lighthouse.Frontend/` when StrykerJS runs, because the
  frontend config resolves it relative to the working directory. It is gitignored there
  (`**/vitest.stryker*.ts`), so the committed copy in this folder is the source of truth.
- The two runs were done sequentially. Stryker rebuilds the solution and reruns the suite hundreds of
  times; a concurrent `dotnet test` or a second Stryker against the same `bin/` crashes the test host
  mid-run, and the failures read as real test failures.
