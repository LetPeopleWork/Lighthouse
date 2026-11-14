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
const THE_LONG_ITEM = "TZ-301";

test("a delivery lead sees the one exceptionally long closed item named in the Cycle Time header and listed on request", async ({
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

	await expect.poll(() => scatter.getOutlierChipText()).toContain("1 outlier");

	await scatter.openOutlierList();

	await expect(scatter.outlierListDialog).toBeVisible();
	await expect(scatter.outlierRow(THE_LONG_ITEM)).toBeVisible();
});
