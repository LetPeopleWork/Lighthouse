import { describe, expect, it } from "vitest";
import type {
	ForecastGrade,
	RealityCheckCell,
} from "../../../models/Forecasts/RealityCheckResult";
import {
	ACCURATE_MEANS,
	badgeWords,
	CREDIT_LEAD,
	cellComparison,
	dayInDigits,
	EXPLANATION_NAME,
	FULL_MONTE,
	gradeLegendCopy,
	headlineExplanation,
	heldGlyph,
	heldUpMeans,
	horizonLabel,
	type LevelRowFacts,
	LOADING,
	legendTitles,
	levelBarName,
	levelRowText,
	listOf,
	notCheckedCell,
	percentShown,
	periodActual,
	periodHeader,
	realityCheckHeadline,
	scenariosLeftOut,
	sufficiencyReasonCopy,
	tableRegionName,
	triggerExplanation,
	type UnrunChecks,
	unevaluableRowCopy,
	unevaluatedSentence,
	whyChecksCouldNotRun,
	windowRowLabel,
} from "./realityCheckCopy";
import type { WindowBadgeState } from "./realityCheckGrading";

const LADDER = [14, 30, 60, 90];

describe("listOf", () => {
	it.each([
		{ items: [], text: "" },
		{ items: [30], text: "30" },
		{ items: [14, 30], text: "14 and 30" },
		{ items: [14, 30, 60, 90], text: "14, 30, 60 and 90" },
	])("reads $items as '$text'", ({ items, text }) => {
		expect(listOf(items)).toBe(text);
	});
});

describe("unevaluatedSentence", () => {
	it("says nothing when every window could be checked", () => {
		expect(unevaluatedSentence([])).toBeNull();
	});

	it("names a single window as not checked", () => {
		expect(unevaluatedSentence([30])).toBe(
			"The 30-day sampling window could not be checked, so it is not counted either way.",
		);
	});

	it("names several windows as not checked", () => {
		expect(unevaluatedSentence([14, 30])).toBe(
			"The 14-day and 30-day sampling windows could not be checked, so they are not counted either way.",
		);
	});
});

// A minimum other than the shipped 5, so a literal 5 in the copy would show.
const MINIMUM_ACTIVE_DAYS = 7;

const ticketTerms = (key: string) =>
	key === "workItems" ? "Tickets" : `unexpected ${key}`;

const someUnrunChecks = (
	overrides: Partial<UnrunChecks> = {},
): UnrunChecks => ({
	checkCount: 4,
	windowDays: [14],
	minimumActiveDays: MINIMUM_ACTIVE_DAYS,
	getTerm: ticketTerms,
	...overrides,
});

const aCell = (
	samplingWindowDays: number,
	reason: RealityCheckCell["sufficiency"]["reason"],
): RealityCheckCell => ({
	horizonDays: 7,
	samplingWindowDays,
	scoredPeriodStart: "2026-09-15",
	scoredPeriodEnd: "2026-09-22",
	historyWindowStart: "2026-08-01",
	historyWindowEnd: "2026-09-15",
	sufficiency: {
		isSufficient: reason === "Sufficient",
		reason,
		daysWithCompletedWork: 2,
	},
	forecast: null,
	actualCompleted: null,
	outcome: null,
	levelOutcomes: null,
});

describe("sufficiencyReasonCopy", () => {
	it("tells thin history with the minimum it was held to and the renamed work items", () => {
		expect(sufficiencyReasonCopy.TooFewActiveDays(someUnrunChecks())).toBe(
			"4 checks on the 14-day sampling window had fewer than 7 days with completed Tickets to draw on.",
		);
	});

	it("tells a forecast that could not be worked out without borrowing the thin-history words", () => {
		const sentence = sufficiencyReasonCopy.DegenerateForecast(
			someUnrunChecks({ checkCount: 1, windowDays: [30, 60] }),
		);
		expect(sentence).toBe(
			"For 1 check on the 30-day and 60-day sampling windows, no forecast could be worked out from the history.",
		);
		expect(sentence).not.toMatch(/days with completed/i);
	});

	it("says nothing of a check that could run", () => {
		expect(sufficiencyReasonCopy.Sufficient(someUnrunChecks())).toBeNull();
	});
});

describe("whyChecksCouldNotRun", () => {
	it("gives one sentence per reason, naming its windows in ladder order and counting only its own checks", () => {
		const cells = [
			aCell(60, "TooFewActiveDays"),
			aCell(14, "DegenerateForecast"),
			aCell(14, "TooFewActiveDays"),
			aCell(30, "Sufficient"),
			aCell(14, "TooFewActiveDays"),
		];

		expect(
			whyChecksCouldNotRun(cells, LADDER, MINIMUM_ACTIVE_DAYS, ticketTerms),
		).toEqual([
			"3 checks on the 14-day and 60-day sampling windows had fewer than 7 days with completed Tickets to draw on.",
			"For 1 check on the 14-day sampling window, no forecast could be worked out from the history.",
		]);
	});

	it("has nothing to say when every check could run", () => {
		expect(
			whyChecksCouldNotRun(
				[aCell(14, "Sufficient")],
				LADDER,
				MINIMUM_ACTIVE_DAYS,
				ticketTerms,
			),
		).toEqual([]);
	});
});

describe("LOADING", () => {
	it("says the check is crunching the numbers, with one ellipsis character and no name", () => {
		expect(LOADING).toBe("Crunching the numbers…");
	});
});

describe("triggerExplanation", () => {
	it("says what the check does in the instance's words for Team and Work Items", () => {
		const squadTerms = (key: string) =>
			({ team: "Squad", workItems: "Tickets" })[key] ?? `unexpected ${key}`;

		expect(triggerExplanation(squadTerms)).toBe(
			"Replays this Squad's recent forecasts — each recent period, forecast from several sampling windows — and compares every one with the Tickets actually completed.",
		);
	});
});

describe("horizonLabel", () => {
	it.each([
		{ horizonDays: 7, expected: "1 week" },
		{ horizonDays: 14, expected: "2 weeks" },
		{ horizonDays: 28, expected: "4 weeks" },
		{ horizonDays: 56, expected: "8 weeks" },
		{ horizonDays: 10, expected: "10 days" },
	])(
		"names a $horizonDays-day horizon $expected",
		({ horizonDays, expected }) => {
			expect(horizonLabel(horizonDays)).toBe(expected);
		},
	);
});

const workItemTerms = (key: string) =>
	({ workItem: "Ticket", workItems: "Tickets" })[key] ?? `unexpected ${key}`;

describe("periodActual", () => {
	it.each([
		{ actual: 42, expected: "42 Tickets completed" },
		{ actual: 1, expected: "1 Ticket completed" },
		{ actual: 0, expected: "0 Tickets completed" },
	])(
		"says $expected in the instance's words for Work Items",
		({ actual, expected }) => {
			expect(periodActual(actual, workItemTerms)).toBe(expected);
		},
	);
});

describe("dayInDigits", () => {
	it.each([
		{ isoDay: "2026-09-26", locale: "de-CH", expected: "26.09.2026" },
		{ isoDay: "2026-01-01", locale: "de-CH", expected: "01.01.2026" },
		{ isoDay: "2026-09-26", locale: "en-US", expected: "09/26/2026" },
		{ isoDay: "2026-12-31", locale: "en-US", expected: "12/31/2026" },
	])(
		"reads $isoDay in $locale as the calendar day it names, $expected",
		({ isoDay, locale, expected }) => {
			expect(dayInDigits(isoDay, locale)).toBe(expected);
		},
	);

	it("leaves a string that is not a day as it came", () => {
		expect(dayInDigits("not a day", "en-US")).toBe("not a day");
	});
});

describe("periodHeader", () => {
	const aWeekEnding = (actualCompleted: number) => ({
		horizonDays: 7,
		scoredPeriodStart: "2026-09-16",
		scoredPeriodEnd: "2026-09-22",
		actualCompleted,
	});

	it.each([
		{
			locale: "de-CH",
			actual: 6,
			expected:
				"Forecast Horizon: 1 week (16.09.2026 – 22.09.2026) – 6 Tickets completed",
		},
		{
			locale: "en-US",
			actual: 6,
			expected:
				"Forecast Horizon: 1 week (09/16/2026 – 09/22/2026) – 6 Tickets completed",
		},
		{
			locale: "de-CH",
			actual: 1,
			expected:
				"Forecast Horizon: 1 week (16.09.2026 – 22.09.2026) – 1 Ticket completed",
		},
	])(
		"reads $actual completed in $locale as one whole line",
		({ locale, actual, expected }) => {
			expect(periodHeader(7, aWeekEnding(actual), workItemTerms, locale)).toBe(
				expected,
			);
		},
	);

	it("heads a period with no scored period by its horizon alone", () => {
		expect(periodHeader(56, undefined, workItemTerms, "de-CH")).toBe(
			"Forecast Horizon: 8 weeks",
		);
	});
});

describe("windowRowLabel", () => {
	it.each([
		{ windowDays: 45, isYourSetting: true, expected: "45 days, your setting" },
		{ windowDays: 30, isYourSetting: false, expected: "30 days" },
	])(
		"labels the $windowDays-day window $expected",
		({ windowDays, isYourSetting, expected }) => {
			expect(windowRowLabel(windowDays, isYourSetting)).toBe(expected);
		},
	);
});

describe("tableRegionName", () => {
	it("names the table's scrolling region by the Team's own name and what the table shows", () => {
		expect(tableRegionName("Ocean Explorer")).toBe(
			"Forecasts checked for Ocean Explorer, by forecast horizon and sampling window",
		);
	});
});

const someRowFacts = (daysWithCompletedWork: number) => ({
	daysWithCompletedWork,
	minimumActiveDays: MINIMUM_ACTIVE_DAYS,
	getTerm: ticketTerms,
});

describe("unevaluableRowCopy", () => {
	it("tells thin history with the days it had, the minimum it was held to and the renamed work items", () => {
		expect(unevaluableRowCopy.TooFewActiveDays(someRowFacts(3))).toBe(
			"Not enough history in this window to check: 3 days with completed Tickets, 7 needed.",
		);
	});

	it("counts a single day in the singular", () => {
		expect(unevaluableRowCopy.TooFewActiveDays(someRowFacts(1))).toMatch(
			/: 1 day with completed Tickets,/,
		);
	});

	it("tells a forecast that could not be worked out without borrowing the thin-history words", () => {
		const sentence = unevaluableRowCopy.DegenerateForecast(someRowFacts(20));
		expect(sentence).toBe(
			"No forecast could be worked out from the history in this window.",
		);
		expect(sentence).not.toMatch(/not enough history|days with completed/i);
	});

	it("still says something when a check that could run brought no forecast back", () => {
		expect(unevaluableRowCopy.Sufficient(someRowFacts(30))).toBe(
			"No forecast came back for this check.",
		);
	});
});

describe("heldGlyph", () => {
	it.each([
		{ held: true, glyph: "✓" },
		{ held: false, glyph: "✗" },
	])("marks held $held with $glyph", ({ held, glyph }) => {
		expect(heldGlyph(held)).toBe(glyph);
	});
});

describe("notCheckedCell", () => {
	it("names a level the check left out", () => {
		expect(notCheckedCell).toBe("Not checked at this confidence level.");
	});
});

describe("cellComparison", () => {
	it.each([
		{
			facts: {
				forecastValue: 40,
				actualCompleted: 42,
				miss: 2,
				percentOfActual: 5,
			},
			name: "Closed 2 Tickets more (42) than forecasted (40). Forecast off by +5%",
		},
		{
			facts: {
				forecastValue: 14,
				actualCompleted: 15,
				miss: 1,
				percentOfActual: 7,
			},
			name: "Closed 1 Ticket more (15) than forecasted (14). Forecast off by +7%",
		},
		{
			facts: {
				forecastValue: 21,
				actualCompleted: 14,
				miss: -7,
				percentOfActual: 50,
			},
			name: "Closed 7 Tickets fewer (14) than forecasted (21). Forecast off by −50%",
		},
		{
			facts: {
				forecastValue: 4,
				actualCompleted: 3,
				miss: -1,
				percentOfActual: 33,
			},
			name: "Closed 1 Ticket fewer (3) than forecasted (4). Forecast off by −33%",
		},
		{
			facts: {
				forecastValue: 22,
				actualCompleted: 22,
				miss: 0,
				percentOfActual: 0,
			},
			name: "Closed exactly the forecasted 22 Tickets.",
		},
		{
			facts: {
				forecastValue: 1,
				actualCompleted: 1,
				miss: 0,
				percentOfActual: 0,
			},
			name: "Closed exactly the forecasted 1 Ticket.",
		},
		{
			facts: {
				forecastValue: 0,
				actualCompleted: 0,
				miss: 0,
				percentOfActual: null,
			},
			name: "Closed no Tickets, exactly as forecasted.",
		},
		{
			facts: {
				forecastValue: 2,
				actualCompleted: 0,
				miss: -2,
				percentOfActual: null,
			},
			name: "Closed 2 Tickets fewer (0) than forecasted (2). No percentage — nothing was completed.",
		},
		{
			facts: {
				forecastValue: 1,
				actualCompleted: 0,
				miss: -1,
				percentOfActual: null,
			},
			name: "Closed 1 Ticket fewer (0) than forecasted (1). No percentage — nothing was completed.",
		},
	])("reads $name", ({ facts, name }) => {
		expect(cellComparison(facts, workItemTerms)).toBe(name);
	});

	it("never writes a hyphen for fewer completed", () => {
		expect(
			cellComparison(
				{
					forecastValue: 48,
					actualCompleted: 42,
					miss: -6,
					percentOfActual: 14,
				},
				workItemTerms,
			),
		).not.toContain("-");
	});
});

describe("percentShown", () => {
	it.each([
		{ percent: 0, shown: "0%" },
		{ percent: 26, shown: "26%" },
	])("writes $percent as $shown", ({ percent, shown }) => {
		expect(percentShown(percent)).toBe(shown);
	});
});

describe("gradeLegendCopy", () => {
	it.each<{ grade: ForecastGrade; label: string }>([
		{ grade: "HeldWithin10", label: "within 10%" },
		{ grade: "Held10To25", label: "10–25% off" },
		{ grade: "HeldOver25", label: "more than 25% off" },
		{ grade: "NotHeldWithin10", label: "within 10%" },
		{ grade: "NotHeld10To25", label: "10–25% off" },
		{ grade: "NotHeldOver25", label: "more than 25% off" },
	])("labels $grade's band $label", ({ grade, label }) => {
		expect(gradeLegendCopy[grade]).toBe(label);
	});
});

describe("legendTitles", () => {
	it("titles the held row and the missed row", () => {
		expect(legendTitles).toEqual({
			held: "Forecast held",
			missed: "Forecast missed",
		});
	});
});

describe("badgeWords", () => {
	it.each<{ state: WindowBadgeState; days: number; words: string }>([
		{ state: "Fine", days: 30, words: "Your 30-day sampling window: fine" },
		{
			state: "DidNotHoldUp",
			days: 14,
			words: "Your 14-day sampling window: did not hold up",
		},
		{
			state: "NoWindowHeldUp",
			days: 30,
			words: "Your 30-day sampling window: did not hold up (no window did)",
		},
		{
			state: "CouldNotBeChecked",
			days: 60,
			words: "Your 60-day sampling window: could not be checked",
		},
		{
			state: "FixedDates",
			days: 30,
			words:
				"Your Squad forecasts from fixed dates: sampling window not tested",
		},
		{
			state: "NotAPositiveLength",
			days: 0,
			words: "Your sampling window: not tested (not a positive number of days)",
		},
	])('$state reads "$words"', ({ state, days, words }) => {
		expect(badgeWords[state](days, squadTerms)).toBe(words);
	});
});

describe("heldUpMeans", () => {
	it("says what it takes for a sampling window to hold up", () => {
		expect(heldUpMeans).toBe(
			"A sampling window held up when its 95th forecast held in more than half of the checks that could be run on it.",
		);
	});
});

const mariasEightyFifth: LevelRowFacts = {
	confidenceLevel: 85,
	levelName: "Confident",
	heldCount: 15,
	runsEvaluated: 16,
	share: 94,
	withinTenPercent: 3,
};

const notTested: LevelRowFacts = {
	confidenceLevel: 50,
	levelName: "Risky",
	heldCount: 0,
	runsEvaluated: 0,
	share: null,
	withinTenPercent: 0,
};

describe("levelRowText", () => {
	it("reads the held share, the counts and the accurate checks", () => {
		expect(levelRowText(mariasEightyFifth)).toBe("94% (15 of 16) · 3 accurate");
	});

	it("says a level no check could test was not tested", () => {
		expect(levelRowText(notTested)).toBe("Not tested — no check could run");
	});
});

describe("levelBarName", () => {
	it("names the bar by its level, its held share and counts, the rate it should hold at and its accurate checks", () => {
		expect(levelBarName(mariasEightyFifth)).toBe(
			"85th Confident: held in 94% of checks (15 of 16), expected about 85%; 3 accurate within 10%",
		);
	});

	it("names the bar of a level no check could test as not tested", () => {
		expect(levelBarName(notTested)).toBe(
			"50th Risky: not tested, no check could run",
		);
	});
});

describe("realityCheckHeadline", () => {
	it.each([
		{
			attempted: 16,
			evaluated: 16,
			scores: 64,
			headline: "Backtested 16 scenarios · 64 forecasts",
		},
		{
			attempted: 16,
			evaluated: 12,
			scores: 48,
			headline: "Backtested 12 of 16 scenarios · 48 forecasts",
		},
		{
			attempted: 16,
			evaluated: 0,
			scores: 0,
			headline: "None of the 16 scenarios could be backtested",
		},
		{
			attempted: 1,
			evaluated: 1,
			scores: 1,
			headline: "Backtested 1 scenario · 1 forecast",
		},
		{
			attempted: 2,
			evaluated: 1,
			scores: 4,
			headline: "Backtested 1 of 2 scenarios · 4 forecasts",
		},
		{
			attempted: 1,
			evaluated: 0,
			scores: 0,
			headline: "None of the 1 scenario could be backtested",
		},
	])(
		'$evaluated of $attempted scenarios and $scores forecasts read "$headline"',
		({ attempted, evaluated, scores, headline }) => {
			expect(
				realityCheckHeadline({
					runsAttempted: attempted,
					runsEvaluated: evaluated,
					levelsPerRun: 4,
					scoresEvaluated: scores,
				}),
			).toBe(headline);
		},
	);
});

const squadTerms = (key: string) =>
	key === "team" ? "Squad" : `unexpected ${key}`;

const theLadder = (
	runsEvaluated: number,
	scoresEvaluated: number,
	cells: RealityCheckCell[] = [],
) => ({
	sampledHorizonDays: [7, 14, 28, 56],
	sampledWindowDays: LADDER,
	minimumActiveDays: MINIMUM_ACTIVE_DAYS,
	cells,
	denominator: {
		runsAttempted: 16,
		runsEvaluated,
		levelsPerRun: 4,
		scoresEvaluated,
	},
});

describe("headlineExplanation", () => {
	it("says what a scenario is, how many forecasts the scenarios give, and what accurate means", () => {
		expect(headlineExplanation(theLadder(16, 64), squadTerms)).toEqual([
			"Each scenario replays one forecast: a recent period (1 week, 2 weeks, 4 weeks and 8 weeks, each ending today), forecast from one sampling window of the history before it, then compared with what the Squad actually completed. Each is read at 4 confidence levels, so 16 scenarios give 64 forecasts.",
			"Accurate means within 10% of what was completed, whether the forecast held or not.",
		]);
	});

	it("speaks of a single scenario, level and forecast in the singular", () => {
		const [scenario] = headlineExplanation(
			{
				sampledHorizonDays: [7],
				sampledWindowDays: [30],
				minimumActiveDays: MINIMUM_ACTIVE_DAYS,
				cells: [],
				denominator: {
					runsAttempted: 1,
					runsEvaluated: 1,
					levelsPerRun: 1,
					scoresEvaluated: 1,
				},
			},
			squadTerms,
		);

		expect(scenario).toBe(
			"Each scenario replays one forecast: a recent period (1 week, each ending today), forecast from one sampling window of the history before it, then compared with what the Squad actually completed. Each is read at 1 confidence level, so 1 scenario gives 1 forecast.",
		);
	});

	it("leaves out how many forecasts the scenarios give when none could be backtested", () => {
		const [scenario] = headlineExplanation(theLadder(0, 0), squadTerms);

		expect(scenario).toBe(
			"Each scenario replays one forecast: a recent period (1 week, 2 weeks, 4 weeks and 8 weeks, each ending today), forecast from one sampling window of the history before it, then compared with what the Squad actually completed. Each is read at 4 confidence levels.",
		);
	});
});

describe("scenariosLeftOut", () => {
	it("counts the scenarios that could not run out of those attempted", () => {
		expect(scenariosLeftOut(theLadder(12, 48).denominator)).toBe(
			"4 of the 16 scenarios could not run and are left out of every count.",
		);
	});

	it("says nothing when every scenario ran", () => {
		expect(scenariosLeftOut(theLadder(16, 64).denominator)).toBeNull();
	});
});

describe("headlineExplanation when some scenarios could not run", () => {
	const squadAndTicketTerms = (key: string) =>
		key === "team" ? "Squad" : ticketTerms(key);

	it("says how many were left out and why, between what the scenarios give and what accurate means", () => {
		const thinFourteenDays = [7, 14, 28, 56].map(() =>
			aCell(14, "TooFewActiveDays"),
		);

		expect(
			headlineExplanation(
				theLadder(12, 48, thinFourteenDays),
				squadAndTicketTerms,
			),
		).toEqual([
			"Each scenario replays one forecast: a recent period (1 week, 2 weeks, 4 weeks and 8 weeks, each ending today), forecast from one sampling window of the history before it, then compared with what the Squad actually completed. Each is read at 4 confidence levels, so 12 scenarios give 48 forecasts.",
			"4 of the 16 scenarios could not run and are left out of every count. 4 checks on the 14-day sampling window had fewer than 7 days with completed Tickets to draw on.",
			"Accurate means within 10% of what was completed, whether the forecast held or not.",
		]);
	});
});

describe("the explanation's name and its credit", () => {
	it("names the explanation, says what accurate means and credits Nick Brown's article, linked", () => {
		expect(EXPLANATION_NAME).toBe("About these numbers");
		expect(ACCURATE_MEANS).toBe(
			"Accurate means within 10% of what was completed, whether the forecast held or not.",
		);
		expect(CREDIT_LEAD).toBe("Inspired by Nick Brown's article");
		expect(FULL_MONTE).toEqual({
			title: "The Full Monte",
			url: "https://medium.com/asos-techblog/the-full-monte-901d721b8532",
		});
	});
});
