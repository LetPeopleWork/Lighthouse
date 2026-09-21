import type { IEntityReference } from "../../../../../../models/EntityReference";
import type {
	IFeature,
	IFeatureTeamForecast,
} from "../../../../../../models/Feature";
import { getColorMapForKeys } from "../../../../../../utils/theme/colors";
import {
	type DeliveryTimeline,
	forecastDateAt,
	type TimelinePercentile,
} from "./deliveryTimelineModel";

/**
 * Which contributing Teams get a row of their own beneath a Feature's bar, what each row spans,
 * what it is called and coloured, and what has to be said on the bar about the Teams that get none.
 *
 * Pure, and library-free on purpose: the chart paints to a canvas this environment does not have,
 * so a decision made inside the adapter could only be checked by looking at the screen. Everything
 * here is arithmetic and naming, and all of it is asserted directly.
 *
 * It sits beside `buildDeliveryTimeline` rather than inside it. A Feature's own bar is not touched
 * by any of this — not its dates, not its object, not its row — and computing lanes from the built
 * timeline rather than within it is what makes that true by construction rather than by care.
 */

/** One Team's own span under the Feature it contributes to. */
export interface TeamLane {
	featureId: number;
	teamId: number;
	teamName: string;
	start: Date;
	end: Date;
	color: string;
}

/**
 * What a Feature's bar says about a Team that has no row of its own.
 *
 * `isWarning` is never true. On the one shape that is actually reachable the Feature is
 * forecasting correctly and the Team simply has nothing left to do, so raising amber there would
 * spend the alarm on something that is not wrong — and a symbol raised over nothing is a symbol
 * readers learn to stop reading.
 */
export interface UnlanedTeamNote {
	text: string;
	isWarning: boolean;
}

export interface UnlanedTeam {
	/**
	 * Carried because the name does not identify a Team. Every Team this Portfolio cannot name is
	 * given the same fallback phrase, so two of them on one Feature are one string twice - and a
	 * bar keyed on that string shows one of them and drops the other, which is the very thing
	 * naming them was for.
	 */
	teamId: number;
	teamName: string;
	note: UnlanedTeamNote;
}

/** A Team and the colour that stands for it everywhere on this chart. */
export interface TeamColour {
	teamId: number;
	teamName: string;
	color: string;
}

export interface DeliveryTeamLanes {
	lanes: TeamLane[];
	/** The Teams with nothing to draw, keyed by the Feature whose bar has to name them. */
	unlanedTeams: Map<number, UnlanedTeam[]>;
	/**
	 * The one Team a Feature's own bar wears, for the Features only one Team works on. Its dates
	 * are untouched; what it gains is the Team's colour and the Team's name.
	 */
	barTeams: Map<number, TeamColour>;
	/** Every Team carrying a colour on the chart, once each, in the order the rows read. */
	legend: TeamColour[];
	/** Whether showing the Teams would change anything at all here, which is what offers the control. */
	canShowTeams: boolean;
}

export interface TeamLaneTerms {
	teamTerm: string;
	portfolioTerm: string;
}

/** One Team is a bar. Two are a question about which of them drives which end. */
const MINIMUM_TEAMS_TO_SPLIT = 2;

/** A Team, as this Portfolio can or cannot name it, before it has been given a colour. */
interface NamedTeam {
	teamId: number;
	teamName: string;
	isNamed: boolean;
}

/** A row before it has been given its place in the reading order and its colour. */
interface PendingLane extends NamedTeam {
	featureId: number;
	start: Date;
	end: Date;
}

// "Lane" is what this code calls a row and what the design documents call one. It is not a word
// the product uses anywhere a reader can see - the switch offers to show Teams, and the sentence
// above the chart talks about the Teams beneath a bar - so it stays out of the sentence too.
const noLaneSentence = (teamName: string) =>
	`No forecast for ${teamName}, so it is not shown separately.`;

export function buildDeliveryTeamLanes(
	features: IFeature[],
	timeline: DeliveryTimeline,
	teams: IEntityReference[],
	percentile: TimelinePercentile,
	terms: TeamLaneTerms,
): DeliveryTeamLanes {
	const { lanes, unlanedTeams, soleTeams } = gatherTeams(
		timeline,
		contributorIndex(features),
		percentile,
		teamNamer(teams, terms),
	);

	const colourOf = teamColourLookup(everyTeamInReach(features, teams));

	// Every Team that carries a colour on this picture: the Teams with lanes of their own and the
	// Teams whose single-Feature bar wears their colour instead. This set is what the key to the
	// colours lists, so it follows the probability being read - unlike the colours themselves.
	const colourCarriers = [...lanes, ...soleTeams.values()];

	return {
		lanes: lanes.map((lane) => colouredLane(lane, colourOf)),
		unlanedTeams,
		barTeams: colouredBarTeams(soleTeams, colourOf),
		legend: legendOver(colourCarriers, colourOf),
		// Offered whenever turning it on would put something on the chart that is not there now:
		// a lane, a note about a Team that has none, or a Team's name on a bar it has to itself.
		canShowTeams:
			lanes.length > 0 || unlanedTeams.size > 0 || soleTeams.size > 0,
	};
}

/**
 * Field by field rather than spread-and-add.
 *
 * A spread would carry `isNamed` out with it, and a lane arriving with a working note nothing
 * outside this file has any use for is how such a note becomes part of a contract by accident.
 */
const colouredLane = (
	lane: PendingLane,
	colourOf: (teamId: number) => string,
): TeamLane => ({
	featureId: lane.featureId,
	teamId: lane.teamId,
	teamName: lane.teamName,
	start: lane.start,
	end: lane.end,
	color: colourOf(lane.teamId),
});

const colouredBarTeams = (
	soleTeams: Map<number, NamedTeam>,
	colourOf: (teamId: number) => string,
): Map<number, TeamColour> =>
	new Map(
		[...soleTeams].map(([featureId, team]) => [
			featureId,
			withColour(team, colourOf),
		]),
	);

function gatherTeams(
	timeline: DeliveryTimeline,
	contributorsOf: (featureId: number) => IFeatureTeamForecast[],
	percentile: TimelinePercentile,
	nameOf: (teamId: number) => NamedTeam,
) {
	const grouped: PendingLane[][] = [];
	const unlanedTeams = new Map<number, UnlanedTeam[]>();
	const soleTeams = new Map<number, NamedTeam>();

	// Walked in the order the timeline placed them, which is the board's own order: a row sits
	// directly under its Feature, so re-sorting anything here separates the two.
	for (const bar of timeline.bars) {
		const contributors = contributorsOf(bar.featureId);

		if (contributors.length < MINIMUM_TEAMS_TO_SPLIT) {
			const sole = soleTeamOf(contributors, nameOf);

			if (sole) {
				soleTeams.set(bar.featureId, sole);
			}

			continue;
		}

		const split = splitByForecast(
			bar.featureId,
			contributors,
			percentile,
			nameOf,
		);

		grouped.push(inReadingOrder(split.laned));

		if (split.unlaned.length > 0) {
			unlanedTeams.set(bar.featureId, split.unlaned);
		}
	}

	return { lanes: grouped.flat(), unlanedTeams, soleTeams };
}

/**
 * A Feature only one Team works on is never split into rows, and the reason is geometric rather
 * than a matter of taste: with one Team, the earliest and the latest across the Teams are that
 * Team, so a row of its own would be a second bar drawn exactly where the first one is.
 *
 * What such a bar is missing is not the span, it is *which Team* — which a bar never carries. So
 * the bar wears that Team's colour and its name instead, and a reader who asks to see the Teams is
 * answered on every Feature rather than on half of them.
 *
 * Only where the Team can be named. A bar already carries its Feature's name, and adding a phrase
 * to it that amounts to "a Team we cannot name" spends the width without answering anything.
 */
function soleTeamOf(
	contributors: IFeatureTeamForecast[],
	nameOf: (teamId: number) => NamedTeam,
): NamedTeam | undefined {
	if (contributors.length !== 1) {
		return undefined;
	}

	const team = nameOf(contributors[0].teamId);

	return team.isNamed ? team : undefined;
}

function contributorIndex(features: IFeature[]) {
	const byId = new Map(
		features.map((feature) => [feature.id, feature.teamForecasts ?? []]),
	);

	return (featureId: number): IFeatureTeamForecast[] =>
		byId.get(featureId) ?? [];
}

/**
 * What a Team is called wherever this chart names one - along its own lane, on the bar of a Feature
 * it has to itself, and in the key to the colours.
 *
 * The names available are the *Portfolio's* Teams, and a Feature can sit in several Portfolios, so
 * a Team on its forecast need not be one this Portfolio lists. Dropping that lane would recreate
 * the silent disagreement the whole split exists to prevent, one level down and harder to notice,
 * so the Team is named for what it is instead.
 */
function teamNamer(teams: IEntityReference[], terms: TeamLaneTerms) {
	const namesById = new Map<number, string>();

	for (const team of teams) {
		if (team.name) {
			namesById.set(team.id, team.name);
		}
	}

	const outsider = `A ${terms.teamTerm} from outside this ${terms.portfolioTerm}`;

	return (teamId: number): NamedTeam => {
		const name = namesById.get(teamId);

		return {
			teamId,
			teamName: name ?? outsider,
			isNamed: name !== undefined,
		};
	};
}

/**
 * A Team gets a lane only when **both** of its own ends resolve at the probability the reader is
 * looking at, which mirrors the two gates a Feature's own bar already passes through.
 *
 * A Team that resolves at one end, or at some other probability, is treated exactly like a Team
 * that resolves at neither: the free edition draws an undated task at a position the data does not
 * support rather than leaving it out, so half a lane would assert a schedule nobody forecast.
 */
function splitByForecast(
	featureId: number,
	contributors: IFeatureTeamForecast[],
	percentile: TimelinePercentile,
	nameOf: (teamId: number) => NamedTeam,
): { laned: PendingLane[]; unlaned: UnlanedTeam[] } {
	const laned: PendingLane[] = [];
	const unlaned: UnlanedTeam[] = [];

	for (const contributor of contributors) {
		const { teamName, isNamed } = nameOf(contributor.teamId);
		const start = forecastDateAt(contributor.startPercentiles, percentile);
		const end = forecastDateAt(contributor.completionPercentiles, percentile);

		if (start && end) {
			laned.push({
				featureId,
				teamId: contributor.teamId,
				teamName,
				isNamed,
				start,
				end,
			});
			continue;
		}

		unlaned.push({
			teamId: contributor.teamId,
			teamName,
			note: { text: noLaneSentence(teamName), isWarning: false },
		});
	}

	return { laned, unlaned };
}

/**
 * Alphabetically by Team, with the Teams this Portfolio cannot name last.
 *
 * By name and never by date, so that moving the probability moves every row without any of them
 * changing places — a reader following one Team down the chart keeps it where it was.
 */
const byTeamName = (left: NamedTeam, right: NamedTeam): number => {
	if (left.isNamed !== right.isNamed) {
		return left.isNamed ? -1 : 1;
	}

	return left.teamName.localeCompare(right.teamName);
};

const inReadingOrder = (lanes: PendingLane[]): PendingLane[] =>
	[...lanes].sort(byTeamName);

/**
 * Every Team this chart could ever have to colour: the ones its Features name, and the ones the
 * Portfolio holds.
 *
 * Deliberately **not** the Teams that turn out to have something drawn at the probability being
 * read, and not the Features the timeline managed to place either - both of those move when the
 * reader moves the control, and the colour must not.
 */
function everyTeamInReach(
	features: IFeature[],
	teams: IEntityReference[],
): number[] {
	const ids = new Set<number>();

	for (const team of teams) {
		ids.add(team.id);
	}

	for (const feature of features) {
		for (const contributor of feature.teamForecasts ?? []) {
			ids.add(contributor.teamId);
		}
	}

	return [...ids];
}

/**
 * One colour per Team, and the same colour whatever the reader is looking at.
 *
 * **The key set has to be one the probability cannot change.** The helper sorts its keys and hands
 * out colours by position, so a set that loses a Team gives every Team after it the colour of its
 * neighbour: move the control from 70 to 95, watch one Team stop resolving, and the rest silently
 * swap colours under a reader the key has just taught to follow them by colour. Built over every
 * Team in reach instead, the same map is produced at every probability - and, since the Portfolio's
 * own Teams are always in it, across the Deliveries of one Portfolio too.
 *
 * Keyed by the Team's id and never by its name. Two Teams this Portfolio cannot name share one
 * fallback phrase, so a name-keyed map would collapse them into a single bucket and paint two
 * different Teams identically. A zero id is safe, since it keys as the string "0" and so survives
 * the helper's own `keys.filter(Boolean)`.
 *
 * Colour is never the only carrier. The Team's name is written along its lane and in the key, so a
 * reader who cannot tell the hues apart loses the grouping shortcut and nothing else.
 */
function teamColourLookup(teamIds: number[]): (teamId: number) => string {
	const colors = getColorMapForKeys(teamIds.map(String));

	return (teamId: number) => colors[String(teamId)];
}

const withColour = (
	team: NamedTeam,
	colourOf: (teamId: number) => string,
): TeamColour => ({
	teamId: team.teamId,
	teamName: team.teamName,
	color: colourOf(team.teamId),
});

/**
 * Every Team carrying a colour, once each.
 *
 * It earns its space rather than decorating: a row is only as wide as its Team's span, so at the
 * widths this chart actually gets, a Team's name along its row is routinely cut to a few
 * characters. The legend is then the only place a colour can be read back to a Team at all.
 */
function legendOver(
	carriers: NamedTeam[],
	colourOf: (teamId: number) => string,
): TeamColour[] {
	const byId = new Map<number, NamedTeam>();

	for (const carrier of carriers) {
		byId.set(carrier.teamId, carrier);
	}

	return [...byId.values()]
		.sort(byTeamName)
		.map((team) => withColour(team, colourOf));
}
