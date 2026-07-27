import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";
import { WorkItemAgingChart } from "../../models/metrics/WorkItemAgingChart";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const AGING_CHART_WIDGET_ID = "aging";

test("flow coach toggles per-state pace bands on and off on the team Work Item Aging chart", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetail.goToMetrics();
	const flowMetricsWidgets = await metrics.switchCategory(
		MetricsCategories.FlowMetrics,
	);
	const agingWidget = await metrics.getWidgetByName(
		MetricsWidgetNames.WorkItemAgingChart,
		flowMetricsWidgets,
	);
	await expect(agingWidget.Widget).toBeVisible();

	const agingChart = new WorkItemAgingChart(page, AGING_CHART_WIDGET_ID);

	await expect
		.poll(() => agingChart.countCycleTimePercentileChips())
		.toBeGreaterThan(0);
	const cycleTimeChipsBefore = await agingChart.countCycleTimePercentileChips();

	await expect.poll(() => agingChart.countPaceBands()).toBe(0);

	await agingChart.togglePacePercentiles();
	await expect.poll(() => agingChart.countPaceBands()).toBeGreaterThan(0);

	await agingChart.togglePacePercentiles();
	await expect.poll(() => agingChart.countPaceBands()).toBe(0);

	await expect
		.poll(() => agingChart.countCycleTimePercentileChips())
		.toBe(cycleTimeChipsBefore);
});

// The portfolio twin of the spec above is gone. It navigated to a portfolio and
// then drove the SAME WorkItemAgingChart component through the SAME toggle — a
// per-owner permutation of one widget, paid for with a full demo re-seed. The
// toggle itself is covered by WorkItemAgingChart.test.tsx and
// useShowPaceBands.test.ts; the portfolio read path by the portfolio
// work-item-age integration tests.
