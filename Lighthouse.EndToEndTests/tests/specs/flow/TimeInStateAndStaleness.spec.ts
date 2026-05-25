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
const DEMO_PORTFOLIO_NAME = "Project Apollo";

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

test("portfolio admin opts staleness in from the Flow Metrics Configuration group; the field is seeded with the portfolio default", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const portfolioDetail = await overviewPage.goToPortfolio(DEMO_PORTFOLIO_NAME);
	const portfolioEdit = await portfolioDetail.editPortfolio();

	await expect(portfolioEdit.legacyFlowSignalsGroupHeader).toHaveCount(0);
	await expect(portfolioEdit.stalenessThresholdField).toHaveCount(0);

	await portfolioEdit.enableStaleness();

	await expect(portfolioEdit.stalenessThresholdField).toBeVisible();
	expect(await portfolioEdit.getStalenessThreshold()).toBe(14);
});

test("flow coach sees stale items turn red in the work-item dialog after enabling staleness with a low threshold", async ({
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
	const workInProgressOverview = await metrics.getWidgetByName(
		"Work In Progress Overview",
		flowOverviewWidgets,
	);
	const workItemsDialog = await workInProgressOverview.openDialog();

	await expect(workItemsDialog.timeInStateColumnHeader).toBeVisible();
	expect(await workItemsDialog.countStaleTimeInStateBadges()).toBeGreaterThan(
		0,
	);
});

test("flow coach sees the Time in State column on the portfolio work-item view", async ({
	page,
	request,
	overviewPage,
}) => {
	test.fixme(
		true,
		"slice 02 US-04: portfolio Time-in-State column renders, but live badges need connector-side Feature transition capture (ADO #5088, deferred in 05-01). Un-fixme once Feature capture lands.",
	);

	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const portfolioDetail = await overviewPage.goToPortfolio(DEMO_PORTFOLIO_NAME);
	const metrics = await portfolioDetail.goToMetrics();
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
	for (const badge of badges) {
		expect(badge).toMatch(/\d+d in .+/);
	}
});

test("flow coach sees the Stale Items widget count a stale item, while a blocked-and-over-threshold item is counted by Blocked not Stale", async ({
	page,
	request,
	overviewPage,
}) => {
	test.fixme(
		true,
		"slice 03 US-06: Stale Items overview widget (count + RAG + view-data) modelled on Blocked Items; blocked-excludes-stale (D10). Un-fixme when US-06 code (StaleOverviewWidget + computeStaleOverviewRag) lands; confirm demo seed has an over-threshold item AND a blocked-over-threshold item.",
	);

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

	const staleWidget = await metrics.getWidgetByName(
		MetricsWidgetNames.StaleItemsOverview,
		flowOverviewWidgets,
	);
	await expect(staleWidget.Widget).toBeVisible();
	expect(await staleWidget.getStaleOverviewCount()).toBeGreaterThan(0);
	expect(await staleWidget.getRagStatus()).not.toBe("");

	const staleDialog = await staleWidget.openDialog();
	await expect(staleDialog.timeInStateColumnHeader).toBeVisible();
	expect(await staleDialog.countStaleTimeInStateBadges()).toBeGreaterThan(0);
});

test("flow coach sees a stale item red in the Work Item Aging Chart and its red Time in State on bubble click; a blocked-over-threshold item is never red-as-stale", async ({
	page,
	request,
	overviewPage,
}) => {
	test.fixme(
		true,
		"slice 03 US-07: aging-chart stale red bubbles + Time-in-State on bubble click (red when stale) + blocked precedence on every surface. Un-fixme when US-07 code (deriveStaleness + chart isStale) lands; confirm demo seed has a stale-not-blocked item and a blocked-over-threshold item.",
	);

	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const teamEdit = await teamDetail.editTeam();
	await teamEdit.enableStaleness();
	await teamEdit.setStalenessThreshold(1);
	const refreshedDetail = await teamEdit.save();

	const metrics = await refreshedDetail.goToMetrics();
	const flowMetricsWidgets = await metrics.switchCategory(
		MetricsCategories.FlowMetrics,
	);
	const agingChart = await metrics.getWidgetByName(
		MetricsWidgetNames.WorkItemAgingChart,
		flowMetricsWidgets,
	);
	await expect(agingChart.Widget).toBeVisible();
	expect(await agingChart.countStaleAgingBubbles()).toBeGreaterThan(0);

	const agingDialog = await agingChart.openDialog();
	await expect(agingDialog.timeInStateColumnHeader).toBeVisible();
	expect(await agingDialog.countStaleTimeInStateBadges()).toBeGreaterThan(0);
});
