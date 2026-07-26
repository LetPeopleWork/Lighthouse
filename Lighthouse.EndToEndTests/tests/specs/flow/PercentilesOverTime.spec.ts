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
	PERCENTILES_OVER_TIME_EMPTY_COPY,
	PERCENTILES_OVER_TIME_RANGE_EMPTY_COPY,
	PercentilesOverTimeWidget,
} from "../../models/metrics/PercentilesOverTimeWidget";

function daysBeforeToday(days: number): Date {
	const date = new Date();
	date.setDate(date.getDate() - days);
	return date;
}

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const EXPECTED_LINES = 4; // 50 / 70 / 85 / 95

// The read-only history endpoint the horizon toggle reads from. Toggling to an
// already-viewed horizon must NOT hit it again (re-plot from cache, no recompute).
const HISTORY_ENDPOINT = "/metrics/percentiles-over-time";

// Work Item Age is horizon-less: the read asks for the metric family and lets the
// backend resolve the horizon-less sentinel.
const AGE_METRIC_TYPE = "WorkItemAge";

test("@walking_skeleton @US-01 flow coach opens the Percentiles Over Time widget and reads a dated CT-30 trend", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);

	// Count every read-only history fetch the widget issues, so the toggle back to
	// an already-viewed horizon can be proven to fire none (no day-by-day recompute).
	let historyRequests = 0;
	page.on("request", (req) => {
		if (req.url().includes(HISTORY_ENDPOINT)) {
			historyRequests++;
		}
	});

	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetail.goToMetrics();
	await metrics.switchCategory(MetricsCategories.Predictability);

	const widget = new PercentilesOverTimeWidget(page);
	await expect(widget.Widget).toBeVisible();

	// The demo connection is flagged for percentile backfill, so the chart is
	// populated — the empty-state placeholder must NOT be showing.
	await expect(widget.emptyState).toHaveCount(0);

	// CT-30 is the default horizon.
	await expect.poll(() => widget.isHorizonSelected(30)).toBe(true);
	expect(await widget.isHorizonSelected(60)).toBe(false);
	expect(await widget.isHorizonSelected(90)).toBe(false);

	// Four dated percentile lines are plotted over the demo window.
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);
	await expect.poll(() => widget.countLegendEntries()).toBe(EXPECTED_LINES);
	await expect.poll(() => widget.countAxisTickLabels()).toBeGreaterThan(1);

	// Red→green colouring: the 50th line is the reddest (risky) and the 95th the
	// greenest (certain) — read off the legend swatches without pinning theme hexes.
	const p50 = await widget.legendSwatchColor(50);
	const p95 = await widget.legendSwatchColor(95);
	expect(p50.r).toBeGreaterThan(p95.r);
	expect(p95.g).toBeGreaterThan(p50.g);

	// Switching CT-30 → CT-60 re-plots that horizon's dated lines from recorded
	// history (a single read-only GET, not a recompute).
	const horizon60Fetched = page.waitForResponse(
		(response) =>
			response.url().includes(HISTORY_ENDPOINT) &&
			response.url().includes("horizon=60") &&
			response.request().method() === "GET",
		{ timeout: 30_000 },
	);
	await widget.selectHorizon(60);
	await horizon60Fetched;
	await expect.poll(() => widget.isHorizonSelected(60)).toBe(true);
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);

	// … and CT-60 → CT-90 likewise.
	const horizon90Fetched = page.waitForResponse(
		(response) =>
			response.url().includes(HISTORY_ENDPOINT) &&
			response.url().includes("horizon=90") &&
			response.request().method() === "GET",
		{ timeout: 30_000 },
	);
	await widget.selectHorizon(90);
	await horizon90Fetched;
	await expect.poll(() => widget.isHorizonSelected(90)).toBe(true);
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);

	// Toggling back to the already-viewed CT-30 re-plots from cache: no new
	// history fetch is issued (the toggle triggers no day-by-day recompute).
	const requestsBeforeReturn = historyRequests;
	await widget.selectHorizon(30);
	await expect.poll(() => widget.isHorizonSelected(30)).toBe(true);
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);
	expect(historyRequests).toBe(requestsBeforeReturn);
});

test("@real-io @driving_adapter @US-03 flow coach reads a dated work item age percentile trend from the WIA tab", async ({
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

	const widget = new PercentilesOverTimeWidget(page);
	await expect(widget.Widget).toBeVisible();

	// The toggle row now offers Age alongside the three cycle-time horizons.
	await expect(widget.ageToggle).toBeVisible();
	await expect(widget.horizonToggle(30)).toBeVisible();
	await expect(widget.horizonToggle(60)).toBeVisible();
	await expect(widget.horizonToggle(90)).toBeVisible();

	// Cycle time is still what the widget opens on — Age is a tab, not the default.
	await expect.poll(() => widget.isHorizonSelected(30)).toBe(true);
	expect(await widget.isAgeSelected()).toBe(false);

	// Selecting Age reads the age series. Age is as-of-today, so the read carries
	// no horizon at all — the tab offers no horizon choice to make.
	const ageFetched = page.waitForResponse(
		(response) =>
			response.url().includes(HISTORY_ENDPOINT) &&
			response.url().includes(`metricType=${AGE_METRIC_TYPE}`) &&
			response.request().method() === "GET",
		{ timeout: 30_000 },
	);
	await widget.selectAge();
	const ageResponse = await ageFetched;
	expect(ageResponse.url()).not.toContain("horizon=");

	await expect.poll(() => widget.isAgeSelected()).toBe(true);
	expect(await widget.isHorizonSelected(30)).toBe(false);

	// Four dated age-percentile lines are plotted over the demo window — the demo
	// connection is backfilled for age too, so no empty state here.
	await expect(widget.emptyState).toHaveCount(0);
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);
	await expect.poll(() => widget.countLegendEntries()).toBe(EXPECTED_LINES);
	await expect.poll(() => widget.countAxisTickLabels()).toBeGreaterThan(1);

	// The red→green ramp behaves exactly as it does on the cycle-time tabs.
	const p50 = await widget.legendSwatchColor(50);
	const p95 = await widget.legendSwatchColor(95);
	expect(p50.r).toBeGreaterThan(p95.r);
	expect(p95.g).toBeGreaterThan(p50.g);

	// Toggling back to the already-viewed CT-30 still re-plots cycle time.
	await widget.selectHorizon(30);
	await expect.poll(() => widget.isHorizonSelected(30)).toBe(true);
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);
});

// A team created but never refreshed has no recorded percentiles at all, and it
// sits on a non-demo connection — only demo connections get backdated snapshots
// (ADR-109 / DDD-4), so this is the honest "fresh team" fixture.
test("@edge @US-03 a fresh team's work item age tab shows the honest forward only empty state", async ({
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

	// Deliberately never refreshed: nothing has recorded a percentile for it.
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(freshTeamName);
	const metrics = await teamDetail.goToMetrics();
	await metrics.switchCategory(MetricsCategories.Predictability);

	const widget = new PercentilesOverTimeWidget(page);
	await expect(widget.Widget).toBeVisible();

	await widget.selectAge();
	await expect.poll(() => widget.isAgeSelected()).toBe(true);

	// Honest forward-only copy, never a broken chart.
	await expect(widget.emptyState).toHaveText(PERCENTILES_OVER_TIME_EMPTY_COPY);
	await expect.poll(() => widget.countChartLines()).toBe(0);
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

	const widget = new PercentilesOverTimeWidget(page);
	await expect(widget.Widget).toBeVisible();
	await expect(widget.emptyState).toHaveCount(0);
	await expect.poll(() => widget.countChartLines()).toBe(EXPECTED_LINES);

	const daysOnDefaultRange = await widget.countPlottedDays();
	expect(daysOnDefaultRange).toBeGreaterThan(1);

	const dateRange = new MetricsDateRange(page);
	await dateRange.applyAndWaitFor(
		daysBeforeToday(7),
		daysBeforeToday(1),
		`${HISTORY_ENDPOINT}?`,
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

	const widget = new PercentilesOverTimeWidget(page);
	await expect(widget.Widget).toBeVisible();

	// Entirely before the demo backfill's earliest day AND ending in the past, so
	// the honest reading is "nothing in this range" — this owner does have history.
	const dateRange = new MetricsDateRange(page);
	await dateRange.applyAndWaitFor(
		daysBeforeToday(60),
		daysBeforeToday(45),
		`${HISTORY_ENDPOINT}?`,
	);
	await metrics.switchCategory(MetricsCategories.Predictability);

	await expect(widget.emptyState).toHaveText(
		PERCENTILES_OVER_TIME_RANGE_EMPTY_COPY,
	);
	await expect.poll(() => widget.countChartLines()).toBe(0);
});
