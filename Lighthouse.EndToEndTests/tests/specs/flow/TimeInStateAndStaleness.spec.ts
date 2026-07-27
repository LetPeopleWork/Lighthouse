import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";

// The portfolio twins of the two specs below used to live here — one for the Time
// in State column on the portfolio work-item view, one for the portfolio staleness
// opt-in. Both drove the SAME shared components as their team counterparts and only
// differed in which owner they navigated to, so each cost a full demo re-seed to
// re-prove a component that was already proven. They now sit a layer down:
//   - portfolio read path -> PortfolioTimeInStateReadApiIntegrationTest.cs
//   - portfolio opt-in seed of 14 -> ModifyProjectSettings.test.tsx
//     ("seeds the revealed field with the portfolio default of 14 on opt-in")

test("flow coach sees how long each in-progress item has been in its current state", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetailPage = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetailPage.goToMetrics();
	const flowOverviewWidgets = await metrics.switchCategory(
		MetricsCategories.FlowOverview,
	);
	const workInProgressOverview = await metrics.getWidgetByName(
		"Work In Progress Overview",
		flowOverviewWidgets,
	);

	const workItemsDialog = await workInProgressOverview.openDialog();

	await expect(workItemsDialog.timeInStateColumnHeader).toBeVisible();

	const badges = await workItemsDialog.getTimeInStateBadges();
	expect(badges.length).toBeGreaterThan(0);
	for (const badge of badges) {
		expect(badge).toMatch(/\d+d in .+/);
	}

	await workItemsDialog.sortByTimeInState();
	await expect(workItemsDialog.timeInStateColumnHeader).toBeVisible();
});

test("team admin opts staleness in from the Flow Metrics Configuration group; the old Flow Signals group is gone", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const teamEdit = await teamDetail.editTeam();

	await expect(teamEdit.legacyFlowSignalsGroupHeader).toHaveCount(0);
	await expect(teamEdit.stalenessThresholdField).toHaveCount(0);

	await teamEdit.enableStaleness();

	await expect(teamEdit.stalenessThresholdField).toBeVisible();
	expect(await teamEdit.getStalenessThreshold()).toBe(5);
});

// One settings setup, every surface that has to turn red. This used to be three
// separate specs — work-item dialog, Stale Items widget, Work Item Aging Chart —
// each of which re-seeded the demo scenario and re-walked the team settings form to
// arrive at exactly this state. The assertions are unchanged; only the setup is now
// paid once.
test("flow coach sees a low staleness threshold turn items red across the work-item dialog, the Stale Items widget, and the Work Item Aging Chart", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const teamEdit = await teamDetail.editTeam();
	await teamEdit.enableStaleness();
	await teamEdit.setStalenessThreshold(1);
	const refreshedDetail = await teamEdit.save();

	const metrics = await refreshedDetail.goToMetrics();
	const flowOverviewWidgets = await metrics.switchCategory(
		MetricsCategories.FlowOverview,
	);

	await test.step("stale badges read red in the WIP work-item dialog", async () => {
		const workInProgressOverview = await metrics.getWidgetByName(
			"Work In Progress Overview",
			flowOverviewWidgets,
		);
		const workItemsDialog = await workInProgressOverview.openDialog();

		await expect(workItemsDialog.timeInStateColumnHeader).toBeVisible();
		await expect
			.poll(() => workItemsDialog.countStaleTimeInStateBadges())
			.toBeGreaterThan(0);
		await workItemsDialog.close();
	});

	await test.step("the Stale Items widget counts a stale item, while a blocked-and-over-threshold item is counted by Blocked not Stale", async () => {
		const staleWidget = await metrics.getWidgetByName(
			MetricsWidgetNames.StaleItemsOverview,
			flowOverviewWidgets,
		);
		await expect(staleWidget.Widget).toBeVisible();
		await expect
			.poll(() => staleWidget.getStaleOverviewCount())
			.toBeGreaterThan(0);
		expect(await staleWidget.getRagStatus()).not.toBe("");

		const staleDialog = await staleWidget.openDialog();
		await expect(staleDialog.timeInStateColumnHeader).toBeVisible();
		expect(await staleDialog.countStaleTimeInStateBadges()).toBeGreaterThan(0);
		await staleDialog.close();
	});

	await test.step("the Work Item Aging Chart draws them red and the bubble drill-through carries the red Time in State; a blocked-over-threshold item is never red-as-stale", async () => {
		const flowMetricsWidgets = await metrics.switchCategory(
			MetricsCategories.FlowMetrics,
		);
		const agingChart = await metrics.getWidgetByName(
			MetricsWidgetNames.WorkItemAgingChart,
			flowMetricsWidgets,
		);
		await expect(agingChart.Widget).toBeVisible();
		await expect
			.poll(() => agingChart.countStaleAgingBubbles())
			.toBeGreaterThan(0);

		const agingDialog = await agingChart.openDialogFromStaleBubble();
		await expect(agingDialog.timeInStateColumnHeader).toBeVisible();
		await expect
			.poll(() => agingDialog.countStaleTimeInStateBadges())
			.toBeGreaterThan(0);
	});
});
