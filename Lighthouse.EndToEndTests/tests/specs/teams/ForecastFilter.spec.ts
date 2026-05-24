import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith (User Story + Bug)
const TEAM_NAME = "Team Zenith";

test("forecast filter: configure exclude-bugs rule on team settings, see quick-settings indicator, and verify the filter toggle is wired on Throughput Run Chart, Throughput PBC, and Predictability Score Details", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(TEAM_NAME);

	const teamEdit = await teamDetail.editTeam();
	await teamEdit.forecastFilterEditor.addExcludeByTypeRule("Bug");
	await expect(teamEdit.forecastFilterEditor.takeEffectHint).toBeVisible();
	const teamDetailAfterSave = await teamEdit.save();

	await teamDetailAfterSave.updateTeamData();
	await waitForBackgroundUpdates(request);
	await page.reload();

	await expect
		.poll(() => teamDetailAfterSave.getThroughputQuickSettingTooltip())
		.toMatch(/Forecast filter active/i);

	const metrics = await teamDetailAfterSave.goToMetrics();

	const flowMetricsWidgets = await metrics.switchCategory(
		MetricsCategories.FlowMetrics,
	);
	const runChart = await metrics.getWidgetByName(
		MetricsWidgetNames.ThroughputRunChart,
		flowMetricsWidgets,
	);
	await expect(runChart.Widget).toBeVisible();
	await expect(runChart.forecastFilterToggle).toBeVisible();
	const runChartBefore = await runChart.snapshotChartContent();
	await runChart.toggleForecastFilter();
	await expect
		.poll(() => runChart.snapshotChartContent())
		.not.toEqual(runChartBefore);

	const predictabilityWidgets = await metrics.switchCategory(
		MetricsCategories.Predictability,
	);

	// The PBC's Average / UNPL / LNPL are rounded to integers, and the
	// Predictability Score's percentile bucket can match between raw and
	// filtered when the filter excludes a small slice of demo throughput.
	// Assert the toggle is wired and its state flips — visible-data changes
	// are guarded at the unit-test / integration layer (see
	// ForecastFilterThroughputChartIntegrationTest in the backend suite).
	const throughputPbc = await metrics.getWidgetByName(
		MetricsWidgetNames.ThroughputProcessBehaviourChart,
		predictabilityWidgets,
	);
	await expect(throughputPbc.Widget).toBeVisible();
	await expect(throughputPbc.forecastFilterToggle).toBeVisible();
	expect(await throughputPbc.isForecastFilterEnabled()).toBe(false);
	await throughputPbc.toggleForecastFilter();
	await expect.poll(() => throughputPbc.isForecastFilterEnabled()).toBe(true);

	const predictabilityScore = await metrics.getWidgetByName(
		MetricsWidgetNames.PredictabilityScoreDetails,
		predictabilityWidgets,
	);
	await expect(predictabilityScore.Widget).toBeVisible();
	await expect(predictabilityScore.forecastFilterToggle).toBeVisible();
	expect(await predictabilityScore.isForecastFilterEnabled()).toBe(false);
	await predictabilityScore.toggleForecastFilter();
	await expect
		.poll(() => predictabilityScore.isForecastFilterEnabled())
		.toBe(true);
});
