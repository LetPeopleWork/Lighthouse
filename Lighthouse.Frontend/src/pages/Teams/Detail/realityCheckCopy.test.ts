import { describe, expect, it } from "vitest";
import {
	DETERMINATIONS,
	LEVEL_READINGS,
	NOT_TESTED_REASONS,
	type RealityCheckDenominator,
	type RealityCheckLevelCoverage,
	type RealityCheckSoundWindow,
	STANDINGS,
} from "../../../models/Forecasts/RealityCheckResult";
import {
	denominatorStatement,
	determinationCopy,
	findings,
	levelLine,
	levelReadingCopy,
	listOf,
	notTestedReasonCopy,
	regionOf,
	standingCopy,
	unevaluatedSentence,
	type VerdictFacts,
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
