import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { createTeam } from "../../helpers/api/teams";
import { createAzureDevOpsConnection } from "../../helpers/api/workTrackingSystemConnections";
import { generateRandomName } from "../../helpers/names";
import {
	MetricsCategories,
	MetricsDateRange,
} from "../../models/metrics/MetricsPage";
import {
	PBC_LIMIT_LINES,
	PBC_OVER_TIME_EMPTY_COPY,
	PBC_OVER_TIME_RANGE_EMPTY_COPY,
	PbcOverTimeWidget,
} from "../../models/metrics/PbcOverTimeWidget";

const PBC_OVER_TIME_ENDPOINT = "/metrics/process-behavior-over-time?";

function daysBeforeToday(days: number): Date {
	const date = new Date();
	date.setDate(date.getDate() - days);
	return date;
}

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const EXPECTED_LINES = PBC_LIMIT_LINES.length; // UNPL / Average / LNPL

// The over-time chart plots the three limits AS the series, so it separates
// them by COLOUR and draws every line solid — the point-in-time chart's neutral
// dashes would leave three near-identical greys, unreadable in dark mode.
const SOLID: number[] = [];
const SOLID_BORDER = "solid";

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

	// Each limit is drawn solid, in its own colour, and its legend swatch shows
	// that same colour — the legend has to match what is plotted.
	const strokes: string[] = [];
	for (const line of PBC_LIMIT_LINES) {
		expect(await widget.isLimitLineInLegend(line)).toBe(true);
		expect(await widget.limitLineDashPattern(line)).toEqual(SOLID);

		const stroke = await widget.limitLineStrokeColor(line);
		expect(stroke).toBeTruthy();
		strokes.push(stroke);

		const swatch = await widget.legendSwatchRule(line);
		expect(swatch.style).toBe(SOLID_BORDER);
		expect(swatch.color).toBe(stroke);
	}

	// Three DISTINCT colours: colour is the only channel separating the limits,
	// so a shared stroke would make the band unreadable.
	expect(new Set(strokes).size).toBe(EXPECTED_LINES);
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

// Slice 03b (US-06): the dashboard date pickers now apply to this widget. The demo
// backfill covers [today-14, today-1], so a ~7-day window inside it plots strictly
// fewer days than the default 30-day window, and a window that ends before the
// backfill begins is honestly empty rather than "nothing recorded yet".
test("@real-io @driving_adapter @US-06 narrowing the dashboard range re-plots fewer recorded days", async ({
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
	await expect(widget.emptyState).toHaveCount(0);
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);

	const daysOnDefaultRange = await widget.countPlottedDays();
	expect(daysOnDefaultRange).toBeGreaterThan(1);

	const dateRange = new MetricsDateRange(page);
	await dateRange.applyAndWaitFor(
		daysBeforeToday(7),
		daysBeforeToday(1),
		PBC_OVER_TIME_ENDPOINT,
	);
	await metrics.switchCategory(MetricsCategories.Predictability);
	await expect(widget.Widget).toBeVisible();

	// Wait for the chart to actually PAINT before measuring. countPlottedDays()
	// returns 0 while the series is still loading, and 0 satisfies toBeLessThan —
	// polling the day count directly would pass on the loading sample and the
	// assertion below would hold even with the date filter deleted from the backend.
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);

	// Fewer recorded days inside the narrower window — the pickers actually apply.
	// Bounded on BOTH sides: > 0 proves we measured a painted chart, not an empty one.
	const daysOnNarrowedRange = await widget.countPlottedDays();
	expect(daysOnNarrowedRange).toBeGreaterThan(0);
	expect(daysOnNarrowedRange).toBeLessThan(daysOnDefaultRange);
	await expect(widget.emptyState).toHaveCount(0);
});

test("@edge @US-06 a range that ends before recording began says so, instead of blaming forward-only recording", async ({
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

	// Entirely before the demo backfill's earliest day AND ending in the past, so
	// the honest reading is "nothing in this range" — this owner does have history.
	const dateRange = new MetricsDateRange(page);
	await dateRange.applyAndWaitFor(
		daysBeforeToday(60),
		daysBeforeToday(45),
		PBC_OVER_TIME_ENDPOINT,
	);
	await metrics.switchCategory(MetricsCategories.Predictability);

	await expect(widget.emptyState).toHaveText(PBC_OVER_TIME_RANGE_EMPTY_COPY);
	await expect.poll(() => widget.countChartLines()).toBe(0);
});
