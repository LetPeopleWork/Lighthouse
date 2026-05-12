# CI Learnings

Durable rules derived from CI / SonarCloud failures on `Build And Deploy Lighthouse`. Append a new entry every time `/clean-ci` resolves a failure. Read this file before touching code in the related area.

Each entry follows:

```
### YYYY-MM-DD — <short title>
- **Symptom**: what CI / Sonar reported (rule key, error excerpt, job name).
- **Root cause**: the actual reason, in one sentence.
- **Fix**: what was changed (file:line is enough; the commit has the diff).
- **Rule going forward**: a single declarative do/don't sentence future-Claude can apply BEFORE writing similar code.
```

## Formatting & linting

_None yet._

## Build & compile

_None yet._

## Tests

_None yet._

## SonarCloud — Backend (LetPeopleWork_Lighthouse)

### 2026-05-12 — CA1861: do not pass constant arrays inline to repeated method calls
- **Symptom**: Sonar gate failed with `new_violations = 4` on `LetPeopleWork_Lighthouse`. All four violations were rule `external_roslyn:CA1861` ("Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array") at `Lighthouse.Backend.Tests/Services/Implementation/Authorization/CreateRightsAcceptanceTest.cs:440,452,628,634`.
- **Root cause**: NUnit test bodies passed `new[] { 10, 20, 30, 40 }` and `new[] { 10 }` inline to `GetReadableTeamIdsAsync` and `Is.EquivalentTo` in two different tests. Sonar's analyzer treats "called repeatedly" as "appears more than once in the file" and flags every inline allocation, even in test fixtures where each method runs once per test.
- **Fix**: Hoisted both arrays to `private static readonly int[]` fields (`AllSeededTeamIds`, `CreatorVisibleTeamIds`) at the class level and replaced the four inline literals with the named references.
- **Rule going forward**: In C# test classes, any constant array literal (`new[] { ... }` or collection-expression `[...]`) that appears in more than one test method MUST be declared as a `private static readonly` field at the class level. The same applies to `Is.EquivalentTo`, `Contains`, and any other API that takes an `IEnumerable<T>`. CA1861 will fail the SonarCloud gate even at INFO severity because the project's gate condition is `new_violations = 0` on new code.

## SonarCloud — Frontend (LetPeopleWork_Lighthouse_Frontend)

_None yet._

## EF migrations

_None yet._

## Infra & flakes

_None yet._
