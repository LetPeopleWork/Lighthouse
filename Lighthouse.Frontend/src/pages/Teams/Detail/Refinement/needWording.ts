import type { RefinementVerdict } from "../../../../models/Refinement/Refinement";
import { formatDayAndDate } from "./nextRefinementWording";

/** What the server knows about the ready count against the range, with the words the Team uses. */
export interface NeedFacts {
	verdict: RefinementVerdict;
	readyCount: number;
	low: number;
	high: number;
	teamName: string;
	workItemTerm: string;
	workItemsTerm: string;
	refinementTerm: string;
}

/** The likelihoods the ends of the range are read at, with the word the Team uses for a Team. */
export interface LikelihoodFacts {
	lowPercentile: number;
	highPercentile: number;
	teamTerm: string;
}

/** Where the range comes from, with the words the Team uses. */
export interface NeedOriginFacts extends LikelihoodFacts {
	teamName: string;
	horizonWorkingDays: number;
	cycleStart: Date;
	cycleEnd: Date;
	throughputTerm: string;
	refinementsTerm: string;
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

const describeBelow = (facts: NeedFacts): string =>
	`below ${theRange(facts)} ${workItemsTermFor(facts)} ${facts.teamName} is likely to pull until the ${facts.refinementTerm} after. Refine ${howManyMore(facts)} more.`;

const describeIn = (facts: NeedFacts): string => {
	const where = isOneNumber(facts)
		? `exactly the ${facts.low} likely to be pulled`
		: `in ${theRange(facts)}`;
	return `${where}. Nothing more needs refining by then.`;
};

const describeAbove = (facts: NeedFacts): string => {
	const where = isOneNumber(facts)
		? `above the ${facts.high} likely to be pulled`
		: `above ${theRange(facts)}`;
	return `${where}. Stop refining: nothing more is needed by then.`;
};

const VERDICT_WORDING: Record<RefinementVerdict, (facts: NeedFacts) => string> =
	{
		Below: describeBelow,
		In: describeIn,
		Above: describeAbove,
	};

/**
 * "3 ready — below the range of 5–8 Work Items Team Gravity is likely to pull until the Refinement after. Refine 2 to 5 more."
 * The need covers one cycle: from the next Refinement, which the message names in its title, to the one after it.
 */
export const describeNeed = (facts: NeedFacts): string =>
	`${facts.readyCount} ready — ${VERDICT_WORDING[facts.verdict](facts)}`;

/** "The Team pulls at least the low end with 50% likelihood, and more than the high end with only 15% likelihood." */
export const describeLikelihoods = ({
	lowPercentile,
	highPercentile,
	teamTerm,
}: LikelihoodFacts): string => {
	// A How Many forecast read at a percentile p is the count reached in 100 - p of every 100 runs.
	const lowEndLikelihood = 100 - lowPercentile;
	const highEndLikelihood = 100 - highPercentile;
	return (
		`The ${teamTerm} pulls at least the low end with ${lowEndLikelihood}% likelihood, ` +
		`and more than the high end with only ${highEndLikelihood}% likelihood.`
	);
};

/** Where the range comes from: the Team's Throughput, forecast over the working days between two Refinements in a row. */
export const describeNeedOrigin = (facts: NeedOriginFacts): string => {
	const workingDays =
		facts.horizonWorkingDays === 1 ? "working day" : "working days";
	const start = formatDayAndDate(facts.cycleStart);
	const end = formatDayAndDate(facts.cycleEnd);
	return (
		`Based on ${facts.teamName}'s ${facts.throughputTerm}: a How Many forecast for the ${facts.horizonWorkingDays} ${workingDays} between the ${facts.refinementsTerm} on ${start} and ${end}. ` +
		`${describeLikelihoods(facts)} ` +
		"Same forecast as on the Forecasts page."
	);
};
