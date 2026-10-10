import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";

/** "When Will This Be Done?" — seeds Team Zenith, whose demo source carries Story Points. */
const DEMO_SCENARIO_ID = 0;
const TEAM_NAME = "Team Zenith";

const testWithDemo = testWithDemoData(DEMO_SCENARIO_ID);

// Skipped until the demo data gives Team Zenith its Story Points estimation field; until then the
// Estimation chart is not shown on any demo Team.
testWithDemo.skip(
	"@walking_skeleton @driving_port @US-01 a coach clicks a bubble on Team Zenith's Estimation vs. Cycle Time and reads each item's estimate",
	async ({ testData, overviewPage }) => {
		expect(testData.teams.map((team) => team.name)).toContain(TEAM_NAME);

		const teamDetailPage = await overviewPage.goToTeam(TEAM_NAME);
		const metricsPage = await teamDetailPage.goToMetrics();
		const widgets = await metricsPage.switchCategory(
			MetricsCategories.PortfolioAndFeatures,
		);
		const estimationWidget = await metricsPage.getWidgetByName(
			MetricsWidgetNames.EstimationVsCycleTime,
			widgets,
		);

		const dialog = await estimationWidget.openDialogFromBubble();

		await expect(dialog.columnHeader(/^Estimate/)).toBeVisible();
		await expect.poll(() => dialog.countRows()).toBeGreaterThan(0);
		const estimates = await dialog.cellsIn(/^Estimate/);
		await expect(estimates).toHaveCount(await dialog.countRows());
		await expect(estimates.filter({ hasNotText: /\S/ })).toHaveCount(0);
	},
);
