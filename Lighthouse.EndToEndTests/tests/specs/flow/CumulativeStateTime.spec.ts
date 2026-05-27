import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { CumulativeStateTimeChart } from "../../models/metrics/CumulativeStateTimeChart";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const CUMULATIVE_STATE_TIME_WIDGET_ID = "stateTimeCumulative";

test("@walking_skeleton @US-01 delivery lead opens the team cumulative time per state chart and sees bars with completed and ongoing segments", async ({
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
	const widget = await metrics.getWidgetByName(
		MetricsWidgetNames.CumulativeStateTime,
		flowMetricsWidgets,
	);
	await expect(widget.Widget).toBeVisible();

	const chart = new CumulativeStateTimeChart(
		page,
		CUMULATIVE_STATE_TIME_WIDGET_ID,
	);

	await expect.poll(() => chart.countStateBars()).toBeGreaterThan(0);
	await expect.poll(() => chart.countCompletedSegments()).toBeGreaterThan(0);
	await expect.poll(() => chart.countOngoingSegments()).toBeGreaterThan(0);
});

test("@US-04 delivery lead clicks the constraint bar and reads the contributing items, then closes the panel", async ({
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
	const widget = await metrics.getWidgetByName(
		MetricsWidgetNames.CumulativeStateTime,
		flowMetricsWidgets,
	);
	await expect(widget.Widget).toBeVisible();

	const chart = new CumulativeStateTimeChart(
		page,
		CUMULATIVE_STATE_TIME_WIDGET_ID,
	);
	await expect.poll(() => chart.countStateBars()).toBeGreaterThan(0);

	const drillDown = await chart.clickConstraintBar();
	await expect(drillDown.container).toBeVisible();
	await expect.poll(() => drillDown.countRows()).toBeGreaterThan(0);

	await drillDown.close();
	await expect(drillDown.container).not.toBeVisible();
});

test("@US-05 delivery lead scopes the chart to a single selected work item", async ({
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
	const widget = await metrics.getWidgetByName(
		MetricsWidgetNames.CumulativeStateTime,
		flowMetricsWidgets,
	);
	await expect(widget.Widget).toBeVisible();

	const chart = new CumulativeStateTimeChart(
		page,
		CUMULATIVE_STATE_TIME_WIDGET_ID,
	);
	await expect.poll(() => chart.countStateBars()).toBeGreaterThan(0);

	await chart.searchPicker("TZ-");
	await chart.selectFirstPickerOption();

	await expect.poll(() => chart.countSelectedPickerChips()).toBe(1);
	await expect.poll(() => chart.countStateBars()).toBeGreaterThan(0);
});
