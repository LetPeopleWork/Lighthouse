import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";

test("@walking_skeleton the View Data icon on the Total Throughput overview widget drills through to a populated work item dialog", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetail.goToMetrics();

	const overviewWidgets = await metrics.switchCategory(
		MetricsCategories.FlowOverview,
	);
	const throughputWidget = await metrics.getWidgetByName(
		MetricsWidgetNames.TotalThroughput,
		overviewWidgets,
	);

	await expect(throughputWidget.Widget).toBeVisible();

	// Before slice 01 the Total Throughput widget had no buildViewData entry, so
	// WidgetShell rendered no table icon at all — its presence is the drill-through.
	await expect(throughputWidget.ViewDataButton).toBeVisible();

	const dialog = await throughputWidget.openDialog();

	// The throughput data set is the closed items of the selected range, so the
	// dialog title reads "{context} Completed".
	await expect(dialog.title).toContainText("Completed");
	await expect(dialog.rows.first()).toBeVisible();
	expect(await dialog.countRows()).toBeGreaterThan(0);
});
