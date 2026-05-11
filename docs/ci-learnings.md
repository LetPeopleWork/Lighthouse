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

_None yet._

## SonarCloud — Frontend (LetPeopleWork_Lighthouse_Frontend)

_None yet._

## EF migrations

_None yet._

## Infra & flakes

_None yet._
