import type {
	Determination,
	NotTestedReason,
	RealityCheckSoundWindow,
	Standing,
} from "../../../models/Forecasts/RealityCheckResult";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";

type TermGetter = (key: string) => string;

export type Region =
	| { shape: "span"; firstDays: number; lastDays: number }
	| { shape: "list"; windowDays: number[] }
	| { shape: "none" };

export interface VerdictFacts {
	teamName: string;
	region: Region;
	soundWindow: RealityCheckSoundWindow;
}

/**
 * A span is named only when the windows that held up sit next to each other on the ladder the server
 * swept. Naming "between 14 and 90" over a ladder with 45 in it would claim 45 held up when it did not.
 */
export const regionOf = (
	soundWindowDays: readonly number[],
	sampledWindowDays: readonly number[],
): Region => {
	if (soundWindowDays.length === 0) {
		return { shape: "none" };
	}

	const start = sampledWindowDays.indexOf(soundWindowDays[0]);
	const run = sampledWindowDays.slice(start, start + soundWindowDays.length);
	const isContiguousRun =
		start >= 0 &&
		soundWindowDays.length > 1 &&
		run.length === soundWindowDays.length &&
		run.every((days, index) => days === soundWindowDays[index]);

	if (!isContiguousRun) {
		return { shape: "list", windowDays: [...soundWindowDays] };
	}

	return {
		shape: "span",
		firstDays: soundWindowDays[0],
		lastDays: soundWindowDays[soundWindowDays.length - 1],
	};
};

export const listOf = (items: readonly (number | string)[]): string => {
	if (items.length < 2) {
		return items.join("");
	}
	return `${items.slice(0, -1).join(", ")} and ${items[items.length - 1]}`;
};

const noWindowHeldUp = (teamName: string): string =>
	`None of the sampling windows checked held up for ${teamName}.`;

const regionSentence = ({ region, teamName }: VerdictFacts): string => {
	if (region.shape === "span") {
		return `Anything between ${region.firstDays} and ${region.lastDays} days would have behaved about the same for ${teamName}.`;
	}
	if (region.shape === "list") {
		return `Of the sampling windows checked, ${listOf(region.windowDays)} days held up for ${teamName}.`;
	}
	return noWindowHeldUp(teamName);
};

export const determinationCopy: Record<
	Determination,
	(facts: VerdictFacts) => string
> = {
	AllWindowsAlike: regionSentence,
	SomeWindowsSound: regionSentence,
	NoWindowSound: ({ teamName }) => noWindowHeldUp(teamName),
	NotEnoughEvidence: ({ teamName }) =>
		`No sampling window could be checked for ${teamName}, so nothing can be concluded about any of them.`,
};

const regionNoun: Record<Region["shape"], string> = {
	span: "that range",
	list: "that set",
	none: "any range that held up",
};

export const standingCopy: Record<
	Standing,
	(facts: VerdictFacts) => string | null
> = {
	Inside: ({ region, soundWindow }) =>
		soundWindow.determination === "AllWindowsAlike"
			? `Your current ${soundWindow.currentSettingDays} is inside ${regionNoun[region.shape]} — this setting is fine.`
			: `Your current ${soundWindow.currentSettingDays} is inside ${regionNoun[region.shape]}.`,
	Outside: ({ region, soundWindow }) =>
		`Your current ${soundWindow.currentSettingDays} is not inside ${regionNoun[region.shape]}.`,
	NotDetermined: ({ soundWindow }) =>
		`Your current ${soundWindow.currentSettingDays} could not be checked, so nothing can be said about where it stands.`,
	// The check never ran on this setting, so it makes no claim about it; the reason is told instead.
	NotTested: () => null,
};

export const notTestedReasonCopy: Record<
	NotTestedReason,
	(getTerm: TermGetter) => string
> = {
	UsesFixedDates: (getTerm) =>
		`This ${getTerm(TERMINOLOGY_KEYS.TEAM)} forecasts from fixed dates rather than a rolling sampling window, so its own setting was not tested.`,
	NotAPositiveLength: (getTerm) =>
		`This ${getTerm(TERMINOLOGY_KEYS.TEAM)}'s sampling window is not a positive number of days, so its own setting was not tested.`,
};

export const unevaluatedSentence = (
	unevaluatedWindowDays: readonly number[],
): string | null => {
	if (unevaluatedWindowDays.length === 0) {
		return null;
	}
	const windows = listOf(unevaluatedWindowDays.map((days) => `${days}-day`));
	if (unevaluatedWindowDays.length === 1) {
		return `The ${windows} sampling window could not be checked, so it is not counted either way.`;
	}
	return `The ${windows} sampling windows could not be checked, so they are not counted either way.`;
};

/**
 * The first finding, in reading order: where the windows that held up lie, where the setting stands (or
 * why it was not tested), and which windows could not be checked at all.
 */
export const windowVerdict = (
	teamName: string,
	sampledWindowDays: readonly number[],
	soundWindow: RealityCheckSoundWindow,
	getTerm: TermGetter,
): string => {
	const facts: VerdictFacts = {
		teamName,
		region: regionOf(soundWindow.soundWindowDays, sampledWindowDays),
		soundWindow,
	};
	const notTestedReason = soundWindow.currentSettingNotTestedReason;

	return [
		determinationCopy[soundWindow.determination](facts),
		standingCopy[soundWindow.currentSettingStanding](facts),
		notTestedReason === null
			? null
			: notTestedReasonCopy[notTestedReason](getTerm),
		unevaluatedSentence(soundWindow.unevaluatedWindowDays),
	]
		.filter((sentence): sentence is string => sentence !== null)
		.join(" ");
};
