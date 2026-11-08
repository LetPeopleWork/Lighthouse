#!/bin/sh
cd "$(git rev-parse --show-toplevel)" || exit 1
F=Lighthouse.Frontend/src/tests/timeBudget
echo "HEAD: $(git rev-parse HEAD)"
git grep -n -F -e 'Time budget:' -e ' offenders' HEAD -- $F/timeBudgetReporter.ts
git grep -n -F -e 'listed here with why it is slow' HEAD -- $F/slowTests.ts
git grep -n -F -e 'BUDGET_SECONDS = ' -e 'FAST_BUDGET_SECONDS = ' HEAD -- Lighthouse.Frontend/vitest.config.ts
echo "--- git diff --stat HEAD (nothing below means no difference)"
git diff --stat HEAD -- $F/timeBudgetReporter.ts $F/slowTests.ts Lighthouse.Frontend/vitest.config.ts
echo "--- end"
