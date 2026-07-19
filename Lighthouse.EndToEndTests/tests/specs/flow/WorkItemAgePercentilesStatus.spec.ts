import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	configureServiceLevelExpectation,
	findTeamIdByName,
	readInProgressWorkItemAges,
} from "../../helpers/api/teamMetrics";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";
import { ragLabelFor } from "../../models/metrics/RagChip";
import { WorkItemAgePercentilesWidget } from "../../models/metrics/WorkItemAgePercentilesWidget";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";
const SLE_PROBABILITY = 85;

/** A previous period was found and compared, rather than the no-baseline placeholder. */
const MEASURED_TREND_DIRECTIONS = ["up", "down", "flat"];

/**
 * Slice 04 gave Work Item Age Percentiles the same header chrome every other Flow
 * Overview widget carries: a RAG chip and a previous-period trend.
 *
 * The chip's rule (D6-REVISED) bands on absolute counts of in-progress ages against
 * the SLE's day value. Rather than pin whatever colour the demo seeder happens to
 * produce — which would couple this spec to demo-data shape that can legitimately
 * drift — the spec reads the population from the same endpoint the dashboard reads
 * and *derives* an SLE that puts the widget in a chosen band. Two facts do all the
 * work, and neither depends on the seeded ages:
 *
 *   - an SLE one day above the oldest item means nothing is at or over the limit,
 *     which is green by the rule;
 *   - an SLE one day below the second-oldest item means at least two items are over
 *     it (the oldest and the second-oldest), which is red by the rule.
 *
 * Walking the chip through red → green → red on one page is also what stops the
 * assertions passing vacuously: a chip read from a drifted locator, or a chip
 * painted from something other than the live measurement, cannot produce three
 * different readings from the same selector.
 */
test("@US-05 a delivery lead is told to define an SLE, then sees the Work Item Age Percentiles status follow the ageing population", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);

	const teamId = await findTeamIdByName(request, DEMO_TEAM_NAME);
	const inProgressAges = await readInProgressWorkItemAges(
		request,
		teamId,
		new Date(),
	);

	// The derivation below needs a population it can straddle. If the scenario ever
	// stops seeding one, fail here saying so rather than further down on a colour.
	expect(
		inProgressAges.length,
		"the demo scenario must seed at least three in-progress items for the SLE derivation to straddle",
	).toBeGreaterThanOrEqual(3);

	const agesDescending = [...inProgressAges].sort((a, b) => b - a);
	const sleBelowEveryAge = agesDescending[1] - 1;
	const sleAboveEveryAge = agesDescending[0] + 1;

	expect(
		sleBelowEveryAge,
		"an SLE has to be at least one day, so the second-oldest item must be at least two days old",
	).toBeGreaterThanOrEqual(1);

	await page.goto("/");
	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetail.goToMetrics();

	const overviewWidgets = await metrics.switchCategory(
		MetricsCategories.FlowOverview,
	);
	const percentilesWidget = await metrics.getWidgetByName(
		MetricsWidgetNames.WorkItemAgePercentiles,
		overviewWidgets,
	);
	await expect(percentilesWidget.Widget).toBeVisible();

	const widget = new WorkItemAgePercentilesWidget(page);

	// The seeded team has no SLE, so the chip is a prompt rather than a measurement:
	// red, pointing at the setting it needs. This is deliberately the first thing a
	// delivery lead sees.
	await expect(widget.rag.chip).toBeVisible();
	await expect.poll(() => widget.rag.readStatus()).toBe("red");
	expect(await widget.rag.readLabel()).toBe(ragLabelFor("red"));
	expect(await widget.rag.readTipText()).toContain("in settings");

	// Slice 04's other half: the trend chrome renders for this widget, and the seeded
	// history reaches far enough back that it has a previous period to compare against
	// — so the shell owes us a direction, not the no-baseline placeholder. Which
	// direction is data-dependent and deliberately not pinned; "unknown" would mean
	// the MUI arrow glyph moved under us rather than the data changing.
	await expect(percentilesWidget.trendChrome).toBeVisible();
	expect(MEASURED_TREND_DIRECTIONS).toContain(
		await percentilesWidget.getTrendDirection(),
	);

	// Give the team an SLE no item can breach: everything is younger than the limit,
	// which the rule calls green.
	await configureServiceLevelExpectation(
		request,
		teamId,
		SLE_PROBABILITY,
		sleAboveEveryAge,
	);
	await page.reload();
	await metrics.switchCategory(MetricsCategories.FlowOverview);

	await expect.poll(() => widget.rag.readStatus()).toBe("green");
	expect(await widget.rag.readLabel()).toBe(ragLabelFor("green"));
	expect(await widget.rag.readTipText()).toContain("younger than");

	// Now tighten the SLE below the second-oldest item, so more than one item is over
	// it. The rule calls that red — and this time it is a measurement, not a prompt,
	// which the tip has to say.
	await configureServiceLevelExpectation(
		request,
		teamId,
		SLE_PROBABILITY,
		sleBelowEveryAge,
	);
	await page.reload();
	await metrics.switchCategory(MetricsCategories.FlowOverview);

	await expect.poll(() => widget.rag.readStatus()).toBe("red");
	expect(await widget.rag.readLabel()).toBe(ragLabelFor("red"));
	expect(await widget.rag.readTipText()).toContain("older than");
});
