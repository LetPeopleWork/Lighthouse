import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { CycleTimeScatterPlotChart } from "../../models/metrics/CycleTimeScatterPlotChart";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const CYCLE_SCATTER_WIDGET_ID = "cycleScatter";
const NAMED_DEFINITION = "Implementation to Done";

test("@walking_skeleton @premium a delivery lead selects a named cycle time on the scatterplot and the dots re-plot with recomputed percentile lines", async ({
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
		MetricsWidgetNames.CycleTimeScatterplot,
		flowMetricsWidgets,
	);
	await expect(widget.Widget).toBeVisible();

	const scatter = new CycleTimeScatterPlotChart(page, CYCLE_SCATTER_WIDGET_ID);
	await expect.poll(() => scatter.countDots()).toBeGreaterThan(0);

	expect(await scatter.listDefinitionOptions()).toEqual(
		expect.arrayContaining(["Default", NAMED_DEFINITION]),
	);

	const defaultDurations = await scatter.getDotCycleTimes();

	await scatter.selectDefinition(NAMED_DEFINITION);

	await expect
		.poll(() => scatter.getSelectedDefinition())
		.toContain(NAMED_DEFINITION);
	await expect
		.poll(() => scatter.getDotCycleTimes())
		.not.toEqual(defaultDurations);
});
