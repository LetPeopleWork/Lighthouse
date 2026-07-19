import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import {
	MetricsCategories,
	MetricsDateRange,
	MetricsWidgetNames,
	WorkItemAgePercentilesCard,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";

// The demo seeder resolves its {w-N} placeholders against the day it runs, so a
// window measured backwards from today lands on seeded history whichever day the
// suite runs. Ending six weeks back puts the whole window behind us: every item
// that was in progress on the closing day has had six weeks to close, and in this
// scenario all of them did.
const DAYS_BACK_TO_RANGE_END = 42;
const RANGE_LENGTH_IN_DAYS = 30;

// Any item still in progress on the closing day is at least DAYS_BACK_TO_RANGE_END
// calendar days old *today*. Even counting business days only, that is no fewer
// than 30. So a percentile below this ceiling cannot have been measured from
// today — it can only have been measured from the closing day.
const TODAY_ANCHORED_AGE_FLOOR_IN_DAYS = 25;

function daysBeforeToday(days: number): Date {
	const date = new Date();
	date.setDate(date.getDate() - days);
	return date;
}

/**
 * Work Item Age answers "how long has this been in flight", and the answer is
 * only meaningful relative to a day. Reporting a past window used to answer it
 * as of today: survivors were aged to now, and everything that had closed in the
 * meantime projected to age 0 and was dropped by the age > 0 guard. For this
 * scenario's six-weeks-back window that leaves nothing at all, so a retrospective
 * read "no work in progress" over a month in which the team plainly had work.
 *
 * This test pins the corrected reading. Two independent things have to hold, and
 * they fail in different directions:
 *
 *  - The card renders percentiles rather than its empty-state placeholder. That
 *    is the since-closed population being kept rather than zeroed away. It is the
 *    assertion the old behaviour broke outright.
 *  - Every percentile sits below TODAY_ANCHORED_AGE_FLOOR_IN_DAYS. That is the
 *    anchor itself. It is what stops the first assertion from passing vacuously:
 *    a card populated from today-anchored ages would clear "non-zero" easily and
 *    fail here, because those ages cannot be smaller than the gap to today.
 */
test("@US-04 a delivery lead reading a six-week-old window sees Work Item Age as it stood then, including items that have since closed", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
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

	const rangeEnd = daysBeforeToday(DAYS_BACK_TO_RANGE_END);
	const rangeStart = daysBeforeToday(
		DAYS_BACK_TO_RANGE_END + RANGE_LENGTH_IN_DAYS,
	);

	const dateRange = new MetricsDateRange(page);
	await dateRange.apply(rangeStart, rangeEnd);

	const card = new WorkItemAgePercentilesCard(page);
	await expect(card.title).toBeVisible();

	// The population on that day was real work, so the card owes us a table.
	await expect(card.noWorkInProgressPlaceholder).toHaveCount(0);
	await expect(card.percentileValues.first()).toBeVisible();

	const percentiles = await card.getPercentileValues();
	expect(Object.keys(percentiles).length).toBeGreaterThan(0);

	for (const [percentile, ageInDays] of Object.entries(percentiles)) {
		expect(
			ageInDays,
			`${percentile} percentile should report a real age from the window's closing day`,
		).toBeGreaterThan(0);
		expect(
			ageInDays,
			`${percentile} percentile of ${ageInDays} days is too large to have been measured from the closing day — it looks anchored to today`,
		).toBeLessThan(TODAY_ANCHORED_AGE_FLOOR_IN_DAYS);
	}
});
