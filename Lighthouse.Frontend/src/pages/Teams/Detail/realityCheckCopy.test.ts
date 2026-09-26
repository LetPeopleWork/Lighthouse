import { describe, expect, it } from "vitest";
import {
	DETERMINATIONS,
	LEVEL_READINGS,
	NOT_TESTED_REASONS,
	type RealityCheckCell,
	type RealityCheckDenominator,
	type RealityCheckLevelCoverage,
	type RealityCheckSoundWindow,
	STANDINGS,
} from "../../../models/Forecasts/RealityCheckResult";
import {
	actualDescription,
	bandDescription,
	denominatorStatement,
	determinationCopy,
	findings,
	horizonLabel,
	levelLine,
	levelReadingCopy,
	listOf,
	notTestedReasonCopy,
	regionOf,
	standingCopy,
	sufficiencyReasonCopy,
	type UnrunChecks,
	unevaluableRowCopy,
	unevaluatedSentence,
	type VerdictFacts,
	whyChecksCouldNotRun,
	windowVerdict,
} from "./realityCheckCopy";

const LADDER = [14, 30, 60, 90];
const LADDER_WITH_45 = [14, 30, 45, 60, 90];
const BANNED = /\b(best|recommended|optimal|closest|least bad|nick brown)\b/i;
const HARD_CODED_TERMS =
	/\b(team|work items?|throughput|epic|initiative|story)\b/i;

const getTerm = (key: string) =>
	key === "team" ? "Squad" : `unexpected ${key}`;

const aSoundWindow = (
	overrides: Partial<RealityCheckSoundWindow> = {},
): RealityCheckSoundWindow => ({
	soundWindowDays: LADDER,
	unevaluatedWindowDays: [],
	determination: "AllWindowsAlike",
	currentSettingDays: 30,
	currentSettingWasTested: true,
	currentSettingStanding: "Inside",
	currentSettingNotTestedReason: null,
	...overrides,
});

const factsFor = (
	soundWindow: RealityCheckSoundWindow,
	sampledWindowDays: number[] = LADDER,
): VerdictFacts => ({
	teamName: "Ocean Explorer",
	region: regionOf(soundWindow.soundWindowDays, sampledWindowDays),
	soundWindow,
});

describe("regionOf", () => {
	it.each([
		{ sound: LADDER, sampled: LADDER, first: 14, last: 90 },
		{ sound: [30, 60, 90], sampled: LADDER, first: 30, last: 90 },
		{ sound: [14, 30], sampled: LADDER, first: 14, last: 30 },
		{ sound: [30, 45, 60], sampled: LADDER_WITH_45, first: 30, last: 60 },
	])(
		"names a span for a contiguous run $sound of $sampled",
		({ sound, sampled, first, last }) => {
			expect(regionOf(sound, sampled)).toEqual({
				shape: "span",
				firstDays: first,
				lastDays: last,
			});
		},
	);

	it.each([
		{
			why: "a window that did not hold up in the middle",
			sound: [14, 30, 60, 90],
			sampled: LADDER_WITH_45,
		},
		{
			why: "the only window that held up between two that did not",
			sound: [14, 45, 90],
			sampled: LADDER_WITH_45,
		},
		{
			why: "a gap at the low end of the run",
			sound: [14, 60, 90],
			sampled: LADDER,
		},
		{
			why: "a gap at the high end of the run",
			sound: [14, 30, 90],
			sampled: LADDER,
		},
		{ why: "a single window", sound: [30], sampled: LADDER },
		{
			why: "windows the ladder does not hold",
			sound: [7, 14],
			sampled: LADDER,
		},
	])("lists the windows one by one for $why", ({ sound, sampled }) => {
		expect(regionOf(sound, sampled)).toEqual({
			shape: "list",
			windowDays: sound,
		});
	});

	it("names no region when no window held up", () => {
		expect(regionOf([], LADDER)).toEqual({ shape: "none" });
	});
});

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

describe("determinationCopy", () => {
	it("gives every determination its copy", () => {
		expect(Object.keys(determinationCopy).sort()).toEqual(
			[...DETERMINATIONS].sort(),
		);
	});

	it("names the whole ladder as one span when every window is alike", () => {
		expect(determinationCopy.AllWindowsAlike(factsFor(aSoundWindow()))).toBe(
			"Anything between 14 and 90 days would have behaved about the same for Ocean Explorer.",
		);
	});

	it("names a contiguous region as a span", () => {
		const soundWindow = aSoundWindow({
			soundWindowDays: [30, 60, 90],
			determination: "SomeWindowsSound",
		});
		expect(determinationCopy.SomeWindowsSound(factsFor(soundWindow))).toBe(
			"Anything between 30 and 90 days would have behaved about the same for Ocean Explorer.",
		);
	});

	it("lists a region with a hole in it window by window", () => {
		const soundWindow = aSoundWindow({
			soundWindowDays: [14, 30, 60, 90],
			determination: "SomeWindowsSound",
		});
		expect(
			determinationCopy.SomeWindowsSound(factsFor(soundWindow, LADDER_WITH_45)),
		).toBe(
			"Of the sampling windows checked, 14, 30, 60 and 90 days held up for Ocean Explorer.",
		);
	});

	it("says no window held up, naming none", () => {
		const soundWindow = aSoundWindow({
			soundWindowDays: [],
			determination: "NoWindowSound",
		});
		expect(determinationCopy.NoWindowSound(factsFor(soundWindow))).toBe(
			"None of the sampling windows checked held up for Ocean Explorer.",
		);
	});

	it("says nothing can be concluded when no window could be checked", () => {
		const soundWindow = aSoundWindow({
			soundWindowDays: [],
			unevaluatedWindowDays: LADDER,
			determination: "NotEnoughEvidence",
		});
		expect(determinationCopy.NotEnoughEvidence(factsFor(soundWindow))).toBe(
			"No sampling window could be checked for Ocean Explorer, so nothing can be concluded about any of them.",
		);
	});
});

describe("standingCopy", () => {
	it("gives every standing its copy", () => {
		expect(Object.keys(standingCopy).sort()).toEqual([...STANDINGS].sort());
	});

	it("calls the setting fine when it is inside and every window is alike", () => {
		expect(standingCopy.Inside(factsFor(aSoundWindow()))).toBe(
			"Your current 30 is inside that range — this setting is fine.",
		);
	});

	it("says the setting is inside a span without calling it fine when only some windows held up", () => {
		const soundWindow = aSoundWindow({
			soundWindowDays: [30, 60, 90],
			determination: "SomeWindowsSound",
		});
		expect(standingCopy.Inside(factsFor(soundWindow))).toBe(
			"Your current 30 is inside that range.",
		);
	});

	it("says the setting is inside a listed set", () => {
		const soundWindow = aSoundWindow({
			currentSettingDays: 45,
			soundWindowDays: [14, 45, 90],
			determination: "SomeWindowsSound",
		});
		expect(standingCopy.Inside(factsFor(soundWindow, LADDER_WITH_45))).toBe(
			"Your current 45 is inside that set.",
		);
	});

	it.each([
		{ sound: [30, 60, 90], sampled: LADDER, days: 14, noun: "that range" },
		{
			sound: [14, 30, 60, 90],
			sampled: LADDER_WITH_45,
			days: 45,
			noun: "that set",
		},
		{ sound: [], sampled: LADDER, days: 14, noun: "any range that held up" },
	])("says $days is not inside $noun", ({ sound, sampled, days, noun }) => {
		const soundWindow = aSoundWindow({
			currentSettingDays: days,
			soundWindowDays: sound,
			determination: "SomeWindowsSound",
			currentSettingStanding: "Outside",
		});
		expect(standingCopy.Outside(factsFor(soundWindow, sampled))).toBe(
			`Your current ${days} is not inside ${noun}.`,
		);
	});

	it("says a setting whose checks could not run cannot be placed", () => {
		const soundWindow = aSoundWindow({
			currentSettingStanding: "NotDetermined",
		});
		expect(standingCopy.NotDetermined(factsFor(soundWindow))).toBe(
			"Your current 30 could not be checked, so nothing can be said about where it stands.",
		);
	});

	it("makes no claim about a setting that was not tested", () => {
		const soundWindow = aSoundWindow({
			currentSettingDays: 45,
			currentSettingWasTested: false,
			currentSettingStanding: "NotTested",
			currentSettingNotTestedReason: "UsesFixedDates",
		});
		expect(standingCopy.NotTested(factsFor(soundWindow))).toBeNull();
	});
});

describe("notTestedReasonCopy", () => {
	it("gives every reason its copy", () => {
		expect(Object.keys(notTestedReasonCopy).sort()).toEqual(
			[...NOT_TESTED_REASONS].sort(),
		);
	});

	it.each([
		{
			reason: "UsesFixedDates" as const,
			text: "This Squad forecasts from fixed dates rather than a rolling sampling window, so its own setting was not tested.",
		},
		{
			reason: "NotAPositiveLength" as const,
			text: "This Squad's sampling window is not a positive number of days, so its own setting was not tested.",
		},
	])(
		"explains $reason in the instance's own word for the team",
		({ reason, text }) => {
			expect(notTestedReasonCopy[reason](getTerm)).toBe(text);
		},
	);
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

describe("windowVerdict", () => {
	it("reads the region, the setting and the windows not checked, in that order", () => {
		const soundWindow = aSoundWindow({
			currentSettingDays: 60,
			soundWindowDays: [14, 60, 90],
			unevaluatedWindowDays: [30],
			determination: "SomeWindowsSound",
		});
		expect(windowVerdict("Ocean Explorer", LADDER, soundWindow, getTerm)).toBe(
			"Of the sampling windows checked, 14, 60 and 90 days held up for Ocean Explorer. Your current 60 is inside that set. The 30-day sampling window could not be checked, so it is not counted either way.",
		);
	});

	it("gives a setting that was not tested its reason and no current window", () => {
		const soundWindow = aSoundWindow({
			currentSettingDays: 45,
			currentSettingWasTested: false,
			currentSettingStanding: "NotTested",
			currentSettingNotTestedReason: "UsesFixedDates",
		});
		const verdict = windowVerdict(
			"Ocean Explorer",
			LADDER,
			soundWindow,
			getTerm,
		);
		expect(verdict).toBe(
			"Anything between 14 and 90 days would have behaved about the same for Ocean Explorer. This Squad forecasts from fixed dates rather than a rolling sampling window, so its own setting was not tested.",
		);
		expect(verdict).not.toMatch(/current 45/);
	});

	it.each(
		DETERMINATIONS.flatMap((determination) =>
			STANDINGS.map((standing) => ({ determination, standing })),
		),
	)(
		"never names a winner nor hard-codes a renameable term: $determination, $standing",
		({ determination, standing }) => {
			const soundWindow = aSoundWindow({
				determination,
				currentSettingStanding: standing,
				currentSettingNotTestedReason:
					standing === "NotTested" ? "NotAPositiveLength" : null,
				unevaluatedWindowDays: [60],
			});
			const verdict = windowVerdict(
				"Deep Current",
				LADDER,
				soundWindow,
				getTerm,
			);
			expect(verdict).not.toMatch(BANNED);
			expect(verdict).not.toMatch(HARD_CODED_TERMS);
		},
	);
});

const aDenominator = (
	overrides: Partial<RealityCheckDenominator> = {},
): RealityCheckDenominator => ({
	runsAttempted: 16,
	runsEvaluated: 16,
	levelsPerRun: 4,
	scoresEvaluated: 64,
	...overrides,
});

const aLevel = (
	overrides: Partial<RealityCheckLevelCoverage> = {},
): RealityCheckLevelCoverage => ({
	confidenceLevel: 85,
	heldCount: 14,
	expectedHeldCount: 13.6,
	reading: "SometimesHeld",
	...overrides,
});

const RETIRED_WORDS = /\b(beaten|about right|excellen\w*)\b/i;

describe("denominatorStatement", () => {
	it("states the runs, the levels and the scores, then why they are neither independent nor rankable", () => {
		expect(denominatorStatement(aDenominator())).toBe(
			"16 forecast runs were checked, each read at 4 confidence levels — 64 scores in all. The 4 levels of a single run come from the same simulation, so they are not independent of one another. And each run covers a different stretch of real time — every one ends today and reaches back by its own length — so they are not repeated trials of one experiment and should not be ranked against each other.",
		);
	});

	it("counts only the runs that could be evaluated, and says how many of those attempted were left out", () => {
		const statement = denominatorStatement(
			aDenominator({
				runsAttempted: 20,
				runsEvaluated: 12,
				scoresEvaluated: 48,
			}),
		);
		expect(statement).toMatch(
			/^12 forecast runs were checked, each read at 4 confidence levels — 48 scores in all\. 8 of the 20 checks could not run, so they are left out of every count\. /,
		);
	});

	it("says nothing was left out when every attempted run was evaluated", () => {
		expect(denominatorStatement(aDenominator())).not.toMatch(/could not run/);
	});

	it("speaks of a single run in the singular", () => {
		expect(
			denominatorStatement(
				aDenominator({
					runsAttempted: 1,
					runsEvaluated: 1,
					scoresEvaluated: 4,
				}),
			),
		).toMatch(/^1 forecast run was checked, /);
	});
});

describe("levelReadingCopy", () => {
	it("gives a level between the extremes its two counts and nothing more", () => {
		expect(levelLine(aLevel(), 16)).toBe(
			"At 85% the forecast held in 14 of 16 checks, about 14 expected.",
		);
	});

	it("calls a level that never held over-forecasting, beside the count its rate expected", () => {
		expect(
			levelLine(
				aLevel({
					confidenceLevel: 95,
					heldCount: 0,
					expectedHeldCount: 15.2,
					reading: "NeverHeld",
				}),
				16,
			),
		).toBe(
			"At 95% the forecast held in 0 of 16 checks, about 15 expected — it never held, which is over-forecasting.",
		);
	});

	it("calls a level that always held under-forecasting, beside the count its rate expected", () => {
		expect(
			levelLine(
				aLevel({
					confidenceLevel: 50,
					heldCount: 12,
					expectedHeldCount: 6,
					reading: "AlwaysHeld",
				}),
				12,
			),
		).toBe(
			"At 50% the forecast held in 12 of 12 checks, about 6 expected — it held every time, which is under-forecasting.",
		);
	});

	it("says a level with nothing evaluated was not tested, and prints no counts", () => {
		expect(
			levelLine(
				aLevel({
					confidenceLevel: 70,
					heldCount: 0,
					expectedHeldCount: 0,
					reading: "NotEvaluated",
				}),
				0,
			),
		).toBe("At 70% no check could be run, so this level was not tested.");
	});

	it.each([
		{ expected: 13.6, printed: "about 14 expected" },
		{ expected: 11.2, printed: "about 11 expected" },
		{ expected: 8, printed: "about 8 expected" },
	])(
		"prints the server's expected count $expected rounded to a whole check",
		({ expected, printed }) => {
			expect(levelLine(aLevel({ expectedHeldCount: expected }), 16)).toContain(
				printed,
			);
		},
	);

	it.each(LEVEL_READINGS)(
		"never uses a retired word nor a renameable term: %s",
		(reading) => {
			const line = levelReadingCopy[reading](aLevel({ reading }), 16);
			expect(line).toMatch(/^At 85%/);
			expect(line).not.toMatch(RETIRED_WORDS);
			expect(line).not.toMatch(HARD_CODED_TERMS);
		},
	);
});

describe("findings", () => {
	it("reports the window as a setting on the renamed team and the confidence level as not a setting", () => {
		expect(findings(getTerm, 4)).toEqual([
			"The sampling window is a setting on this Squad.",
			"The confidence level is not a setting: it is which of the 4 numbers you choose to quote.",
		]);
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

	it("is folded into the denominator right after the count of checks left out", () => {
		const statement = denominatorStatement(
			aDenominator({
				runsAttempted: 16,
				runsEvaluated: 12,
				scoresEvaluated: 48,
			}),
			whyChecksCouldNotRun(
				[aCell(14, "TooFewActiveDays")],
				LADDER,
				MINIMUM_ACTIVE_DAYS,
				ticketTerms,
			),
		);
		expect(statement).toMatch(
			/4 of the 16 checks could not run, so they are left out of every count\. 1 check on the 14-day sampling window had fewer than 7 days with completed Tickets to draw on\. The 4 levels/,
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

describe("bandDescription", () => {
	it("reads every level's value in the order given", () => {
		expect(
			bandDescription([
				{ probability: 95, value: 31 },
				{ probability: 85, value: 36 },
				{ probability: 70, value: 41 },
				{ probability: 50, value: 48 },
			]),
		).toBe("Forecast: 31 at 95%, 36 at 85%, 41 at 70% and 48 at 50%.");
	});
});

describe("actualDescription", () => {
	it("reads the actual count with the renamed work items", () => {
		expect(actualDescription(42, ticketTerms)).toBe(
			"Actual: 42 Tickets completed.",
		);
	});
});
