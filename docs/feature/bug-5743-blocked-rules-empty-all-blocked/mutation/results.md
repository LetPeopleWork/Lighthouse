# Bug #5743 — mutation testing

Both stacks clear the 80% floor.

| Stack | Score | Killed | Survived |
|---|---|---|---|
| Backend (Stryker.NET 4.14.1) | **89.34%** | 108 | 5 |
| Frontend (StrykerJS) | **89.16%** | 74 | 4 (+5 no coverage) |

The backend run started at **79.51%** and the frontend at **69.44%**. Both gained from tests
written against the survivors, not from moving the goalposts — the frontend `mutate` range was
also narrowed off pre-existing page furniture (the premium gate, field labels, date inputs) that
this change never touched.

## Running them

Neither config is runnable from this folder: `**/stryker-config*.json` is gitignored, and the
backend paths only resolve from the test project.

```
cp docs/feature/bug-5743-blocked-rules-empty-all-blocked/mutation/stryker.5743.backend.json \
   Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.bug5743.json
cd Lighthouse.Backend/Lighthouse.Backend.Tests
TZ=Europe/Zurich dotnet stryker --config-file stryker-config.bug5743.json

cp docs/feature/bug-5743-blocked-rules-empty-all-blocked/mutation/{stryker.5743.frontend.json,vitest.stryker.mutation.ts} \
   Lighthouse.Frontend/
cd Lighthouse.Frontend
pnpm exec stryker run stryker.5743.frontend.json
```

## What the survivors taught

Seven tests came out of the first backend run, and they pin real boundaries rather than the
mutants themselves:

- A rule set with exactly `MaxRules` conditions is valid, and one condition over is not. Same for
  a value of exactly `MaxValueLength`. Both `>` comparisons had been free to become `>=`.
- A rule set is not partially applied. One condition naming an unknown field — or using an
  unknown operator — makes the whole set match nothing, even in OR mode where the sound
  condition would otherwise mark items on its own. Three mutants lived in that gap.
- A portfolio's rules are validated against the feature fields, a team's against the work item
  fields. Nothing had pinned which list each owner gets.

The frontend run produced a unit test for the completeness predicate (empty field, empty
operator, empty value, and whitespace standing in for each), plus two settings tests: deleting
the last rule row clears the stored definition, and switching blocked items off forgets the rows
being edited rather than restoring them on the way back in.

## Survivors left standing

All are equivalent mutants — the mutated branch produces the same observable result:

- `RuleSetValidation.cs:37` and `ForecastFilterRuleService.cs:46` — removing the "nothing stored"
  early return leaves the following deserialize returning null, which the next guard turns into
  the same answer.
- `ForecastFilterRuleService.cs:91`, `:105` — a `ReferenceEquals` fast path and a `GetHashCode`
  body in the work item comparer. Pre-existing code; neither changes a comparison's outcome.
- `BlockedItemService.cs:94` — `IsMultiValue` on a schema this service only uses internally for
  validation, where the flag is never read.
- `WorkItemRules.ts:71`, `:78` — the remaining guard and catch in `parseRuleSet`. Malformed input
  reaches `JSON.parse`, throws, and returns null by the other path.

The first frontend run had six of these on one guard clause; `json.trim() === ""` was redundant
next to `!json`, because whitespace throws in `JSON.parse` and lands in the catch. Deleting the
redundant half was the honest fix, and the remaining pair genuinely cannot be observed.
