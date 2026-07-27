import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { MetricsCategories } from "../../models/metrics/MetricsPage";
import {
	PBC_LIMIT_LINES,
	PBC_PORTFOLIO_METRIC_TYPES,
	PBC_TEAM_METRIC_TYPES,
	PbcOverTimeWidget,
} from "../../models/metrics/PbcOverTimeWidget";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const DEMO_PORTFOLIO_NAME = "Project Apollo";
const EXPECTED_LINES = PBC_LIMIT_LINES.length; // UNPL / Average / LNPL

// A family the recorder persists that is NOT the default selection — used to
// prove the toggle actually switches families rather than only rendering them.
const OTHER_FAMILY = "WorkItemAge" as const;

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

// The fresh-team empty state moved to PredictabilityOverTime.spec.ts: the
// Percentiles Over Time widget needed the identical never-refreshed team on the
// identical page, so the two specs were building the same fixture twice.
// Scenario 14 (US-05 / D8): the toggle row is the one place Feature Size is
// withheld — the wire deliberately answers a team's `?type=FeatureSize` with an
// empty series (pinned at the read port in Slice04ProcessBehaviorMetricTypesScenarios.cs),
// so the scope rule is only visible to a user here. Absence is asserted from the
// OFFERED set, not from a click that times out: a timing-out click is
// indistinguishable from a broken locator.
test("@real-io @driving_adapter @US-05 the family toggle offers every recorded family, and Feature Size only on a portfolio", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);

	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const teamMetrics = await teamDetail.goToMetrics();
	await teamMetrics.switchCategory(MetricsCategories.Predictability);

	const teamWidget = new PbcOverTimeWidget(page);
	await expect(teamWidget.Widget).toBeVisible();
	// Poll for the exact offered set: offeredMetricTypes() answers [] while the
	// toggle row is still mounting, and [] would satisfy a one-sided "no Feature
	// Size" check on the loading sample.
	await expect
		.poll(() => teamWidget.offeredMetricTypes())
		.toEqual([...PBC_TEAM_METRIC_TYPES]);
	await expect(teamWidget.metricToggle("FeatureSize")).toHaveCount(0);

	await page.goto("/");
	const portfolioDetail = await overviewPage.goToPortfolio(DEMO_PORTFOLIO_NAME);
	const portfolioMetrics = await portfolioDetail.goToMetrics();
	await portfolioMetrics.switchCategory(MetricsCategories.Predictability);

	const portfolioWidget = new PbcOverTimeWidget(page);
	await expect(portfolioWidget.Widget).toBeVisible();
	await expect
		.poll(() => portfolioWidget.offeredMetricTypes())
		.toEqual([...PBC_PORTFOLIO_METRIC_TYPES]);
	await expect(portfolioWidget.metricToggle("FeatureSize")).toBeAttached();
});

// Scenario 13 (US-05): the toggle actually switches families rather than only
// rendering six buttons that all draw Throughput.
test("@real-io @driving_adapter @US-05 selecting another family moves the selection and keeps the chart intact", async ({
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
	await expect(widget.metricToggle(OTHER_FAMILY)).toBeAttached();
	await expect.poll(() => widget.isMetricSelected("Throughput")).toBe(true);

	await widget.selectMetric(OTHER_FAMILY);

	// The pressed state MOVES — the previous family releases it.
	await expect.poll(() => widget.isMetricSelected(OTHER_FAMILY)).toBe(true);
	expect(await widget.isMetricSelected("Throughput")).toBe(false);

	// DELIBERATELY NOT asserted here: the three dated lines. The demo backfill
	// (DemoPercentilesBackfillHandler) backdates Throughput ONLY, by maintainer
	// decision — every other family has at most the single row today's refresh
	// recorded, so a dated triple is not available on demo data and asserting one
	// here would be flaky-by-construction. The per-family dated-triple coverage
	// lives at the read port, in
	// Lighthouse.Backend.Tests/API/Integration/PercentilesOverTime/Slice04ProcessBehaviorMetricTypesScenarios.cs.
	// Do NOT "fix" this gap by weakening that test or by extending the demo
	// backfill — either would erase a deliberate decision.
	//
	// What IS asserted: the widget resolves to one of its two legitimate states —
	// a fully plotted limit triple, or the honest empty copy. A broken chart is a
	// third state (a chart region with fewer than three lines and no empty copy),
	// and that is what this poll rejects.
	await expect
		.poll(async () => {
			const lines = await widget.countChartLines();
			if (lines === EXPECTED_LINES) {
				return "plotted";
			}
			if ((await widget.emptyState.count()) === 1) {
				return "honestly empty";
			}
			return `broken: ${lines} of ${EXPECTED_LINES} lines and no empty state`;
		})
		.toMatch(/^(plotted|honestly empty)$/);
});

// The two US-06 date-range specs moved to PredictabilityOverTime.spec.ts. They
// were byte-for-byte the Percentiles Over Time pair with one widget swapped: same
// demo seed, same team, same navigation, same window. Both widgets now get read
// from a single range change there.
