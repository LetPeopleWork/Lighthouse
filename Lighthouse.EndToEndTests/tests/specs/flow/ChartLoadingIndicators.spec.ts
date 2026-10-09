import { test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	MetricsCategories,
	MetricsDateRange,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";

// A window no preset lands on, so picking the preset below is always a real change.
const STARTING_WINDOW_ENDS_DAYS_AGO = 42;
const STARTING_WINDOW_LENGTH_IN_DAYS = 30;
const PICKED_PRESET_DAYS = 90;

function daysBeforeToday(days: number): Date {
	const date = new Date();
	date.setDate(date.getDate() - days);
	return date;
}

// Pending until the charts report whether they have their data; the frames carry no status yet.
test.skip("@walking_skeleton a delivery lead picks the last 90 days and watches a chart wait for the new window, then show it", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetail.goToMetrics();
	const widgets = await metrics.switchCategory(MetricsCategories.FlowOverview);
	const dateRange = new MetricsDateRange(page);
	await dateRange.apply(
		daysBeforeToday(
			STARTING_WINDOW_ENDS_DAYS_AGO + STARTING_WINDOW_LENGTH_IN_DAYS,
		),
		daysBeforeToday(STARTING_WINDOW_ENDS_DAYS_AGO),
	);
	await metrics.waitUntilEveryChartHasLoaded();

	const totalThroughput = await metrics.getWidgetByName(
		MetricsWidgetNames.TotalThroughput,
		widgets,
	);
	const releaseTheNewWindow = await dateRange.holdAnswersForWindowStarting(
		daysBeforeToday(PICKED_PRESET_DAYS),
	);

	await dateRange.selectPresetWithoutWaiting(`Last ${PICKED_PRESET_DAYS} days`);
	await totalThroughput.waitUntilLoading();

	await releaseTheNewWindow();
	await totalThroughput.waitUntilLoaded();
	await metrics.waitUntilEveryChartHasLoaded();
});
