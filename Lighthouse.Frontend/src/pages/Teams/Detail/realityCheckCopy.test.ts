import { describe, expect, it } from "vitest";
import {
	DETERMINATIONS,
	NOT_TESTED_REASONS,
	type RealityCheckSoundWindow,
	STANDINGS,
} from "../../../models/Forecasts/RealityCheckResult";
import {
	determinationCopy,
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
