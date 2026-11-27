import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { MetricsCategories } from "../../models/metrics/MetricsPage";
import { WithinSleWidget } from "../../models/metrics/WithinSleWidget";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";

test("a delivery lead sees the share of closed Work Items within the SLE next to its target and can list those Work Items", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetail.goToMetrics();
	await metrics.switchCategory(MetricsCategories.FlowOverview);

	const withinSle = new WithinSleWidget(page);
	await withinSle.waitUntilLoaded();

	await expect(withinSle.title).toBeVisible();
	await expect(withinSle.share).toBeVisible();
	await expect(withinSle.sentence).toBeVisible();
	await expect(withinSle.target).toBeVisible();

	const closed = await withinSle.readClosedCount();
	const dialog = await withinSle.openViewData();

	await expect(dialog.dialog).toBeVisible();
	await expect(dialog.columnHeader(/Cycle Time/)).toBeVisible();
	expect(
		await dialog.countRows(),
		`View Data listed a different number of Work Items than the ${closed} the share counts`,
	).toBe(closed);
});
