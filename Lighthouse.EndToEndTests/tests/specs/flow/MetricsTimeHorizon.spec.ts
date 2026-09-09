import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { formatLocalDate } from "../../helpers/dates";
import {
	MetricsCategories,
	MetricsDateRange,
} from "../../models/metrics/MetricsPage";

const DEMO_SCENARIO_ID = 0; // "When Will This Be Done?" — seeds Team Zenith deterministically
const DEMO_TEAM_NAME = "Team Zenith";

const TEAM_STEP_DAYS = 7;
const PRESET_LABEL = `Last ${TEAM_STEP_DAYS} days`;
const CLICKS_IN_BURST = 4;

// Somewhere no preset would land, so picking one is a visible move rather than a
// no-op against whatever window this team happens to open on.
const DAYS_BACK_TO_STARTING_WINDOW_END = 42;
const STARTING_WINDOW_LENGTH_IN_DAYS = 30;

function daysBeforeToday(days: number): Date {
	const date = new Date();
	date.setDate(date.getDate() - days);
	return date;
}

// The header formats with date-fns `dd MMM yyyy`, which abbreviates September to
// "Sep". Node's own en-GB formatter writes "Sept" for the same month, so the months
// are spelled out here rather than left to whatever ICU data the runner ships.
const MONTH_ABBREVIATIONS = [
	"Jan",
	"Feb",
	"Mar",
	"Apr",
	"May",
	"Jun",
	"Jul",
	"Aug",
	"Sep",
	"Oct",
	"Nov",
	"Dec",
];

function asTheHeaderReadsIt(date: Date): string {
	const day = String(date.getDate()).padStart(2, "0");
	return `${day} ${MONTH_ABBREVIATIONS[date.getMonth()]} ${date.getFullYear()}`;
}

/**
 * The window controls are pinned behaviour-by-behaviour by the component suite;
 * what no unit test can see is whether they are wired to the dashboard at all.
 * This walks both of them through a real browser against a real backend, once.
 *
 * The second half is the one that needs a browser rather than a hook harness.
 * Clicking the stepper four times in quick succession is meant to fetch the
 * window the reader stopped on, not the three they passed through on the way —
 * and "did not fetch" is only observable on the wire.
 */
test("@walking_skeleton @US-01 @US-02 a delivery lead picks a named window and then walks it back a month in one burst of clicks", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(DEMO_TEAM_NAME);
	const metrics = await teamDetail.goToMetrics();
	await metrics.switchCategory(MetricsCategories.FlowOverview);

	const dateRange = new MetricsDateRange(page);
	await dateRange.apply(
		daysBeforeToday(
			DAYS_BACK_TO_STARTING_WINDOW_END + STARTING_WINDOW_LENGTH_IN_DAYS,
		),
		daysBeforeToday(DAYS_BACK_TO_STARTING_WINDOW_END),
	);

	const presetStart = daysBeforeToday(TEAM_STEP_DAYS);
	const presetEnd = new Date();
	await dateRange.selectPreset(PRESET_LABEL, presetStart);

	// One assertion over the whole label, not one per end: two independent substring checks pass
	// just as happily on a label that reads its window backwards.
	await expect(dateRange.windowLabel).toContainText(
		`${asTheHeaderReadsIt(presetStart)} → ${asTheHeaderReadsIt(presetEnd)}`,
	);

	// Both ends, not one: a preset that wrote only the end would leave the reader on
	// a window whose length is whatever the previous one happened to be.
	const urlAfterPreset = new URL(page.url());
	expect(urlAfterPreset.searchParams.get("startDate")).toBe(
		formatLocalDate(presetStart),
	);
	expect(urlAfterPreset.searchParams.get("endDate")).toBe(
		formatLocalDate(presetEnd),
	);

	await page.waitForLoadState("networkidle");

	const settledEnd = daysBeforeToday(TEAM_STEP_DAYS * CLICKS_IN_BURST);
	const settledStart = daysBeforeToday(TEAM_STEP_DAYS * (CLICKS_IN_BURST + 1));

	const fetchedWindows = await dateRange.recordFetchedWindowsDuring(
		async () => {
			for (let click = 0; click < CLICKS_IN_BURST; click++) {
				await dateRange.stepBackwardButton(TEAM_STEP_DAYS).click();
			}
		},
		settledEnd,
	);

	expect(
		fetchedWindows.filter((day) => day === formatLocalDate(settledEnd)),
		"the window the reader stopped on should have been fetched exactly once",
	).toHaveLength(1);

	for (let click = 1; click < CLICKS_IN_BURST; click++) {
		const dayPassedThrough = formatLocalDate(
			daysBeforeToday(TEAM_STEP_DAYS * click),
		);
		expect(
			fetchedWindows,
			`the window ending ${dayPassedThrough} was only passed through, so it should never have been fetched`,
		).not.toContain(dayPassedThrough);
	}

	const urlAfterBurst = new URL(page.url());
	expect(urlAfterBurst.searchParams.get("startDate")).toBe(
		formatLocalDate(settledStart),
	);
	expect(urlAfterBurst.searchParams.get("endDate")).toBe(
		formatLocalDate(settledEnd),
	);

	await expect(dateRange.windowLabel).toContainText(
		asTheHeaderReadsIt(settledEnd),
	);
});
