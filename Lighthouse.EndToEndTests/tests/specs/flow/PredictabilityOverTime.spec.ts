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
import {
	PERCENTILES_OVER_TIME_EMPTY_COPY,
	PERCENTILES_OVER_TIME_RANGE_EMPTY_COPY,
	PercentilesOverTimeWidget,
} from "../../models/metrics/PercentilesOverTimeWidget";

// Percentiles Over Time and PBC Over Time are two widgets on ONE dashboard
// category, fed by one date range and one demo backfill. Their range and
// empty-state specs were therefore literal twins living in two files: same demo
// seed, same team, same navigation, same window — differing only in which widget
// they read. Each pair is now a single spec that drives BOTH widgets from one
// setup. Nothing is asserted less; the seed and the navigation are paid once.
//
// Per-widget behaviour that is NOT shared (horizon toggle, family toggle, the
// Feature-Size scope rule) stays in PercentilesOverTime.spec.ts / PbcOverTime.spec.ts.

function daysBeforeToday(days: number): Date {
	const date = new Date();
	date.setDate(date.getDate() - days);
	return date;
}

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const PERCENTILE_LINES = 4; // 50 / 70 / 85 / 95
const PBC_LINES = PBC_LIMIT_LINES.length; // UNPL / Average / LNPL

const PERCENTILES_ENDPOINT = "/metrics/percentiles-over-time?";

// A family the PBC recorder persists that is NOT the default selection.
const OTHER_PBC_FAMILY = "WorkItemAge" as const;

// Slice 03b (US-06): the dashboard date pickers apply to both over-time widgets. The
// demo backfill covers [today-14, today-1], so a ~7-day window inside it plots
// strictly fewer days than the default 30-day window.
test("@real-io @driving_adapter @US-06 narrowing the dashboard range re-plots fewer recorded days on both over-time widgets", async ({
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

	const percentiles = new PercentilesOverTimeWidget(page);
	const pbc = new PbcOverTimeWidget(page);

	await expect(percentiles.Widget).toBeVisible();
	await expect(pbc.Widget).toBeVisible();
	await expect(percentiles.emptyState).toHaveCount(0);
	await expect(pbc.emptyState).toHaveCount(0);
	await expect.poll(() => percentiles.countChartLines()).toBe(PERCENTILE_LINES);
	await expect.poll(() => pbc.countChartLines()).toBe(PBC_LINES);

	const percentileDaysOnDefaultRange = await percentiles.countPlottedDays();
	const pbcDaysOnDefaultRange = await pbc.countPlottedDays();
	expect(percentileDaysOnDefaultRange).toBeGreaterThan(1);
	expect(pbcDaysOnDefaultRange).toBeGreaterThan(1);

	// One navigation re-reads both widgets — waiting on either endpoint proves the
	// new window was requested.
	const dateRange = new MetricsDateRange(page);
	await dateRange.applyAndWaitFor(
		daysBeforeToday(7),
		daysBeforeToday(1),
		PERCENTILES_ENDPOINT,
	);
	await metrics.switchCategory(MetricsCategories.Predictability);
	await expect(percentiles.Widget).toBeVisible();
	await expect(pbc.Widget).toBeVisible();

	// Wait for the charts to actually PAINT before measuring. countPlottedDays()
	// returns 0 while a series is still loading, and 0 satisfies toBeLessThan —
	// polling the day count directly would pass on the loading sample and the
	// assertions below would hold even with the date filter deleted from the backend.
	await expect.poll(() => percentiles.countChartLines()).toBe(PERCENTILE_LINES);
	await expect.poll(() => pbc.countChartLines()).toBe(PBC_LINES);

	// Fewer recorded days inside the narrower window — the pickers actually apply.
	// Bounded on BOTH sides: > 0 proves we measured a painted chart, not an empty one.
	const percentileDaysOnNarrowedRange = await percentiles.countPlottedDays();
	expect(percentileDaysOnNarrowedRange).toBeGreaterThan(0);
	expect(percentileDaysOnNarrowedRange).toBeLessThan(
		percentileDaysOnDefaultRange,
	);
	await expect(percentiles.emptyState).toHaveCount(0);

	const pbcDaysOnNarrowedRange = await pbc.countPlottedDays();
	expect(pbcDaysOnNarrowedRange).toBeGreaterThan(0);
	expect(pbcDaysOnNarrowedRange).toBeLessThan(pbcDaysOnDefaultRange);
	await expect(pbc.emptyState).toHaveCount(0);
});

test("@edge @US-06 a range that ends before recording began says so on both over-time widgets, instead of blaming forward-only recording", async ({
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

	const percentiles = new PercentilesOverTimeWidget(page);
	const pbc = new PbcOverTimeWidget(page);
	await expect(percentiles.Widget).toBeVisible();
	await expect(pbc.Widget).toBeVisible();

	// Entirely before the demo backfill's earliest day AND ending in the past, so
	// the honest reading is "nothing in this range" — this owner does have history.
	const dateRange = new MetricsDateRange(page);
	await dateRange.applyAndWaitFor(
		daysBeforeToday(60),
		daysBeforeToday(45),
		PERCENTILES_ENDPOINT,
	);
	await metrics.switchCategory(MetricsCategories.Predictability);

	await expect(percentiles.emptyState).toHaveText(
		PERCENTILES_OVER_TIME_RANGE_EMPTY_COPY,
	);
	await expect.poll(() => percentiles.countChartLines()).toBe(0);

	await expect(pbc.emptyState).toHaveText(PBC_OVER_TIME_RANGE_EMPTY_COPY);
	await expect.poll(() => pbc.countChartLines()).toBe(0);
});

// A team created but never refreshed has recorded no percentiles and no
// process-behaviour limits at all, and it sits on a non-demo connection — only demo
// connections get backdated snapshots (ADR-109 / DDD-4), so this is the honest
// "fresh team" fixture for BOTH widgets.
test("@edge @US-03 @US-04 a fresh team's over-time widgets show the honest forward-only empty state", async ({
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

	// Deliberately never refreshed: nothing has recorded anything for it.
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(freshTeamName);
	const metrics = await teamDetail.goToMetrics();
	await metrics.switchCategory(MetricsCategories.Predictability);

	const percentiles = new PercentilesOverTimeWidget(page);
	await expect(percentiles.Widget).toBeVisible();

	await percentiles.selectAge();
	await expect.poll(() => percentiles.isAgeSelected()).toBe(true);

	// Honest forward-only copy, never a broken chart.
	await expect(percentiles.emptyState).toHaveText(
		PERCENTILES_OVER_TIME_EMPTY_COPY,
	);
	await expect.poll(() => percentiles.countChartLines()).toBe(0);

	const pbc = new PbcOverTimeWidget(page);
	await expect(pbc.Widget).toBeVisible();
	await expect.poll(() => pbc.isMetricSelected("Throughput")).toBe(true);
	await expect(pbc.emptyState).toHaveText(PBC_OVER_TIME_EMPTY_COPY);
	await expect.poll(() => pbc.countChartLines()).toBe(0);
	await expect(pbc.legend).toHaveCount(0);

	// Slice 04: the same honesty has to hold for a family the recorder only started
	// persisting now. This fresh, never-refreshed team on a non-demo connection has
	// nothing recorded for ANY family, which makes it the only deterministic place
	// to assert the empty copy per family — on the demo team today's refresh records
	// a row for every family.
	await pbc.selectMetric(OTHER_PBC_FAMILY);
	await expect.poll(() => pbc.isMetricSelected(OTHER_PBC_FAMILY)).toBe(true);
	await expect(pbc.emptyState).toHaveText(PBC_OVER_TIME_EMPTY_COPY);
	await expect.poll(() => pbc.countChartLines()).toBe(0);
	await expect(pbc.legend).toHaveCount(0);
});
