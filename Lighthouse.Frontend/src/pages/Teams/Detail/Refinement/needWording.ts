import type { RefinementVerdict } from "../../../../models/Refinement/Refinement";
import { formatDayAndDate } from "./nextRefinementWording";

/** What the server knows about the ready count against the range, with the words the Team uses. */
export interface NeedFacts {
	verdict: RefinementVerdict;
	readyCount: number;
	low: number;
	high: number;
	refinementDay: Date;
	teamName: string;
	workItemTerm: string;
	workItemsTerm: string;
}

/** Where the range comes from, with the words the Team uses. */
export interface NeedOriginFacts {
	teamName: string;
	horizonWorkingDays: number;
	refinementDay: Date;
	lowPercentile: number;
	highPercentile: number;
	teamTerm: string;
	throughputTerm: string;
}

// Equal ends are one number, not a range of one.
const isOneNumber = ({ low, high }: NeedFacts): boolean => low === high;

const howManyMore = ({ readyCount, low, high }: NeedFacts): string => {
	const fewest = low - readyCount;
	const most = high - readyCount;
	return fewest === most ? `${fewest}` : `${fewest} to ${most}`;
};

const theRange = (facts: NeedFacts): string =>
	isOneNumber(facts)
		? `the ${facts.low}`
		: `the range of ${facts.low}–${facts.high}`;

const workItemsTermFor = (facts: NeedFacts): string =>
	isOneNumber(facts) && facts.low === 1
		? facts.workItemTerm
		: facts.workItemsTerm;

const describeBelow = (facts: NeedFacts, day: string): string =>
	`below ${theRange(facts)} ${workItemsTermFor(facts)} ${facts.teamName} is likely to pull before ${day}. Refine ${howManyMore(facts)} more.`;

const describeIn = (facts: NeedFacts, day: string): string => {
	const where = isOneNumber(facts)
		? `exactly the ${facts.low} likely to be pulled`
		: `in ${theRange(facts)}`;
	return `${where}. Nothing more needs refining before ${day}.`;
};

const describeAbove = (facts: NeedFacts, day: string): string => {
	const where = isOneNumber(facts)
		? `above the ${facts.high} likely to be pulled`
		: `above ${theRange(facts)}`;
	return `${where}. Stop refining: nothing more is needed before ${day}.`;
};

const VERDICT_WORDING: Record<
	RefinementVerdict,
	(facts: NeedFacts, day: string) => string
> = {
	Below: describeBelow,
	In: describeIn,
	Above: describeAbove,
};

/** "3 ready — below the range of 5–8 Work Items Team Gravity is likely to pull before Thu 8 Oct. Refine 2 to 5 more." */
export const describeNeed = (facts: NeedFacts): string => {
	const day = formatDayAndDate(facts.refinementDay);
	return `${facts.readyCount} ready — ${VERDICT_WORDING[facts.verdict](facts, day)}`;
};

/** Where the range comes from: the Team's Throughput, forecast over the working days until the next Refinement. */
export const describeNeedOrigin = (facts: NeedOriginFacts): string => {
	const workingDays =
		facts.horizonWorkingDays === 1 ? "working day" : "working days";
	const day = formatDayAndDate(facts.refinementDay);
	// A How Many forecast read at a percentile p is the count reached in 100 - p of every 100 runs.
	const lowEndLikelihood = 100 - facts.lowPercentile;
	const highEndLikelihood = 100 - facts.highPercentile;
	return (
		`Based on ${facts.teamName}'s ${facts.throughputTerm}: a How Many forecast for the ${facts.horizonWorkingDays} ${workingDays} until ${day}. ` +
		`The ${facts.teamTerm} pulls at least the low end with ${lowEndLikelihood}% likelihood, ` +
		`and more than the high end with only ${highEndLikelihood}% likelihood. ` +
		"Same forecast as on the Forecasts page."
	);
};
