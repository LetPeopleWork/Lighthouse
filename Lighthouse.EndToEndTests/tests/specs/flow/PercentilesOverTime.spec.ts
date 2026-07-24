import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { MetricsCategories } from "../../models/metrics/MetricsPage";
import { PercentilesOverTimeWidget } from "../../models/metrics/PercentilesOverTimeWidget";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith + portfolio Project Apollo deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const EXPECTED_LINES = 4; // 50 / 70 / 85 / 95

// The read-only history endpoint the horizon toggle reads from. Toggling to an
// already-viewed horizon must NOT hit it again (re-plot from cache, no recompute).
const HISTORY_ENDPOINT = "/metrics/percentiles-over-time";

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
