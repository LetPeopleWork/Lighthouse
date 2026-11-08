#!/bin/sh
# Searches the tree (git grep, tracked files outside the feature workspace and the skill) for every name the skill gives.
cd "$(git rev-parse --show-toplevel)" || exit 1
for n in "pnpm test:fast" vitest.config.ts BUDGET_SECONDS FAST_BUDGET_SECONDS timeBudgetReporter.ts slowTests.ts "src/tests/timeBudget" \
  computePaceBandRects STATE_BAND_HALF_WIDTH WorkItemAgingChart.tsx getBubbleSize scatterMarkerUtils.tsx SaveStateIndicator \
  generateWorkItem waitPastTheDebounce pastTheAutosaveDelay mockSaveTeamSettings mockGetTeamSettings COULD_NOT_LOAD_MESSAGE \
  WidgetShell 'data-widget-status="ready"' "vi.useFakeTimers({ shouldAdvanceTime: true })" Stryker Playwright "pnpm test" CLAUDE.md \
  PAGE_SIZE RowRangeFooter LIMIT_FACTOR FIRST_TEST_ALLOWANCE_SECONDS "@screenshot" "@auth" "@rbac" "@proxyauth" "src/tests/timeBudget/slowTests.ts" NoSuchNameInTheTreeXyz; do
  c=$(git grep -l -F -e "$n" -- . ':!docs/feature/story-6538-t11-test-writing-guidance' ':!.claude/skills/test-writing' | wc -l)
  printf '%-60s %s files\n' "$n" "$c"
done
git ls-files --error-unmatch Lighthouse.Frontend/src/tests/timeBudget/timeBudgetReporter.ts Lighthouse.Frontend/src/tests/timeBudget/slowTests.ts Lighthouse.Frontend/src/utils/charts/scatterMarkerUtils.tsx Lighthouse.Frontend/src/components/Common/ValidationActions/SaveStateIndicator.tsx Lighthouse.Frontend/src/tests/TestDataProvider.ts Lighthouse.Frontend/src/tests/pastTheAutosaveDelay.ts Lighthouse.Frontend/src/tests/waitPastTheDebounce.ts >/dev/null && echo "paths tracked: yes"
