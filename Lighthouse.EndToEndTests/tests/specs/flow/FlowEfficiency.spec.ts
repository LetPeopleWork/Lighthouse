import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	CumulativeChartFlowEfficiency,
	FlowEfficiencyOverviewTile,
} from "../../models/metrics/FlowEfficiencyWidget";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";
import { WaitStatesEditor } from "../../models/metrics/WaitStatesEditor";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const CUMULATIVE_STATE_TIME_WIDGET_ID = "stateTimeCumulative";

const DEMO_WAIT_STATE = "Verification";

test("@walking_skeleton @US-01 admin marks a wait state and the delivery lead sees flow efficiency on the tile and the cumulative chart", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);

	const teamEdit = await teamDetail.editTeam();
	const waitStates = new WaitStatesEditor(page);
	await waitStates.enable();
	await waitStates.addWaitState(DEMO_WAIT_STATE);
	const detailAfterSave = await teamEdit.save();

	const metrics = await detailAfterSave.goToMetrics();

	const overviewWidgets = await metrics.switchCategory(
		MetricsCategories.FlowOverview,
	);
	const tileWidget = await metrics.getWidgetByName(
		MetricsWidgetNames.FlowEfficiencyOverview,
		overviewWidgets,
	);
	await expect(tileWidget.Widget).toBeVisible();

	const tile = new FlowEfficiencyOverviewTile(page);
	await expect(tile.efficiencyValue).toContainText("%");

	const flowMetricsWidgets = await metrics.switchCategory(
		MetricsCategories.FlowMetrics,
	);
	const chartWidget = await metrics.getWidgetByName(
		MetricsWidgetNames.CumulativeStateTime,
		flowMetricsWidgets,
	);
	await expect(chartWidget.Widget).toBeVisible();

	const chartEfficiency = new CumulativeChartFlowEfficiency(
		page,
		CUMULATIVE_STATE_TIME_WIDGET_ID,
	);
	await expect(chartEfficiency.efficiencyNumber).toContainText("%");
	await expect(chartEfficiency.waitBarLegendEntry).toBeVisible();
	await expect.poll(() => chartEfficiency.countWaitBars()).toBeGreaterThan(0);
});
