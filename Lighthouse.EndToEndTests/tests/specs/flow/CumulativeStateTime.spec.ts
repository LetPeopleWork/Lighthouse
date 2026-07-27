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

// One walk down the widget instead of three specs. US-01, US-04 and US-05 used to
// be separate tests that each re-seeded the demo scenario and re-navigated to the
// same chart on the same team before touching it — the setup cost three times over
// for one widget. The assertions below are unchanged and run in the order a
// delivery lead meets them: read the bars, drill into the constraint, then scope to
// one item.
test("@walking_skeleton @US-01 @US-04 @US-05 delivery lead reads the team cumulative time per state chart, drills into the constraint bar, and scopes it to a single work item", async ({
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

	await test.step("@US-01 the chart shows bars with completed and ongoing segments", async () => {
		await expect.poll(() => chart.countStateBars()).toBeGreaterThan(0);
		await expect.poll(() => chart.countCompletedSegments()).toBeGreaterThan(0);
		await expect.poll(() => chart.countOngoingSegments()).toBeGreaterThan(0);
	});

	await test.step("@US-04 clicking the constraint bar lists the contributing items, and the panel closes again", async () => {
		const drillDown = await chart.clickConstraintBar();
		await expect(drillDown.container).toBeVisible();
		await expect.poll(() => drillDown.countRows()).toBeGreaterThan(0);

		await drillDown.close();
		await expect(drillDown.container).not.toBeVisible();
	});

	await test.step("@US-05 the chart scopes to a single selected work item", async () => {
		await chart.searchPicker("TZ-");
		await chart.selectFirstPickerOption();

		await expect.poll(() => chart.countSelectedPickerChips()).toBe(1);
		await expect.poll(() => chart.countStateBars()).toBeGreaterThan(0);
	});
});
