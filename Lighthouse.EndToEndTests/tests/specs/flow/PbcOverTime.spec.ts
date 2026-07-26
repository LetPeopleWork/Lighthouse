import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { createTeam } from "../../helpers/api/teams";
import { createAzureDevOpsConnection } from "../../helpers/api/workTrackingSystemConnections";
import { generateRandomName } from "../../helpers/names";
import { MetricsCategories } from "../../models/metrics/MetricsPage";
import {
	PBC_LIMIT_LINES,
	PBC_OVER_TIME_EMPTY_COPY,
	PbcOverTimeWidget,
} from "../../models/metrics/PbcOverTimeWidget";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const EXPECTED_LINES = PBC_LIMIT_LINES.length; // UNPL / Average / LNPL

// The dash patterns the point-in-time process-behaviour chart draws with: the
// average is "5 5", the natural limits are "3 3". The over-time widget must
// speak the same visual language rather than inventing its own.
const AVERAGE_DASH = [5, 5];
const LIMIT_DASH = [3, 3];

test("@real-io @driving_adapter @US-04 delivery lead reads dated Throughput process behaviour limits", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);

	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetail.goToMetrics();
	await metrics.switchCategory(MetricsCategories.Predictability);

	const widget = new PbcOverTimeWidget(page);
	await expect(widget.Widget).toBeVisible();

	// Throughput is the family the recorder persists, and the toggle opens on it.
	await expect.poll(() => widget.isMetricSelected("Throughput")).toBe(true);

	// The demo owners are backdated over the last two weeks, so the chart is
	// populated — the forward-only placeholder must NOT be showing.
	await expect(widget.emptyState).toHaveCount(0);

	// Three dated limit lines are plotted across the recorded date range.
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);
	await expect.poll(() => widget.countLegendEntries()).toBe(EXPECTED_LINES);
	await expect.poll(() => widget.countAxisTickLabels()).toBeGreaterThan(1);

	for (const line of PBC_LIMIT_LINES) {
		expect(await widget.isLimitLineInLegend(line)).toBe(true);
	}

	// The limit styling matches the point-in-time chart: average dashed "5 5",
	// the natural limits dashed "3 3", all three in one neutral band colour.
	expect(await widget.limitLineDashPattern("average")).toEqual(AVERAGE_DASH);
	expect(await widget.limitLineDashPattern("unpl")).toEqual(LIMIT_DASH);
	expect(await widget.limitLineDashPattern("lnpl")).toEqual(LIMIT_DASH);

	const averageStroke = await widget.limitLineStrokeColor("average");
	expect(await widget.limitLineStrokeColor("unpl")).toBe(averageStroke);
	expect(await widget.limitLineStrokeColor("lnpl")).toBe(averageStroke);
});

// A team created but never refreshed has recorded no process-behaviour limits
// at all, and it sits on a non-demo connection — only demo connections get
// backdated snapshots (ADR-109 / DDD-4), so this is the honest "fresh team".
test("@edge @US-04 a fresh team's PBC Over Time widget shows the honest empty state", async ({
	page,
	request,
	overviewPage,
}) => {
	const connection = await createAzureDevOpsConnection(
		request,
		generateRandomName(),
	);
	const freshTeamName = generateRandomName();
	await createTeam(
		request,
		freshTeamName,
		connection.id,
		'[System.TeamProject] = "Lighthouse"',
		["User Story", "Bug"],
		{ toDo: ["New"], doing: ["Active"], done: ["Closed"] },
	);

	// Deliberately never refreshed: nothing has recorded a limit triple for it.
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(freshTeamName);
	const metrics = await teamDetail.goToMetrics();
	await metrics.switchCategory(MetricsCategories.Predictability);

	const widget = new PbcOverTimeWidget(page);
	await expect(widget.Widget).toBeVisible();
	await expect.poll(() => widget.isMetricSelected("Throughput")).toBe(true);

	// Honest forward-only copy, never a broken chart.
	await expect(widget.emptyState).toHaveText(PBC_OVER_TIME_EMPTY_COPY);
	await expect.poll(() => widget.countChartLines()).toBe(0);
	await expect(widget.legend).toHaveCount(0);
});
