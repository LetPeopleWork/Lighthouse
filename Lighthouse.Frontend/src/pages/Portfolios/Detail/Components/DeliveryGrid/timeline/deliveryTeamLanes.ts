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
	teamName: string;
	note: UnlanedTeamNote;
}

export interface DeliveryTeamLanes {
	lanes: TeamLane[];
	/** The Teams with nothing to draw, keyed by the Feature whose bar has to name them. */
	unlanedTeams: Map<number, UnlanedTeam[]>;
	/** Whether anything on this chart splits at all, which is what decides if a control is offered. */
	canSplit: boolean;
}

export interface TeamLaneTerms {
	teamTerm: string;
	portfolioTerm: string;
}

/** One Team is a bar. Two are a question about which of them drives which end. */
const MINIMUM_TEAMS_TO_SPLIT = 2;

/** A lane before it has been given its place in the reading order and its colour. */
interface PendingLane {
	featureId: number;
	teamId: number;
	teamName: string;
	isNamed: boolean;
	start: Date;
	end: Date;
}

const noLaneSentence = (teamName: string) =>
	`No forecast for ${teamName}, so it has no lane of its own.`;

export function buildDeliveryTeamLanes(
	features: IFeature[],
	timeline: DeliveryTimeline,
	teams: IEntityReference[],
	percentile: TimelinePercentile,
	terms: TeamLaneTerms,
): DeliveryTeamLanes {
	const contributorsOf = contributorIndex(features);
	const nameOf = teamNamer(teams, terms);

	const grouped: PendingLane[][] = [];
	const unlanedTeams = new Map<number, UnlanedTeam[]>();
	let canSplit = false;

	// Walked in the order the timeline placed them, which is the board's own order: a lane sits
	// directly under its Feature, so re-sorting anything here separates the two.
	for (const bar of timeline.bars) {
		const contributors = contributorsOf(bar.featureId);

		if (contributors.length < MINIMUM_TEAMS_TO_SPLIT) {
			continue;
		}

		// Counted off the forecast rows rather than off the lanes that end up drawn. A Feature
		// whose second Team has no dates still has two Teams, and the reader still has a question
		// the control answers.
		canSplit = true;

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

	return { lanes: coloured(grouped.flat()), unlanedTeams, canSplit };
}

function contributorIndex(features: IFeature[]) {
	const byId = new Map(
		features.map((feature) => [feature.id, feature.teamForecasts ?? []]),
	);

	return (featureId: number): IFeatureTeamForecast[] =>
		byId.get(featureId) ?? [];
}

/**
 * What to write on a Team's lane.
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

	return (teamId: number) => {
		const name = namesById.get(teamId);

		return { teamName: name ?? outsider, isNamed: name !== undefined };
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
	nameOf: (teamId: number) => { teamName: string; isNamed: boolean },
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
			teamName,
			note: { text: noLaneSentence(teamName), isWarning: false },
		});
	}

	return { laned, unlaned };
}

/**
 * Alphabetically by Team, with the Teams this Portfolio cannot name last.
 *
 * By name and never by date, so that moving the probability moves every lane without any of them
 * changing rows — a reader following one Team down the chart keeps it in the same place.
 */
const inReadingOrder = (lanes: PendingLane[]): PendingLane[] =>
	[...lanes].sort((left, right) => {
		if (left.isNamed !== right.isNamed) {
			return left.isNamed ? -1 : 1;
		}

		return left.teamName.localeCompare(right.teamName);
	});

/**
 * One colour per Team for the whole chart.
 *
 * Built once over every Team with a lane anywhere on it, because a map built per Feature would
 * paint one Team two different colours on a single screen. Keyed by the Team's id and never by its
 * name: the helper opens with `keys.filter(Boolean)`, so a Team with no resolvable name would not
 * merely share a bucket with the others — it would be dropped from the map and left with no colour
 * at all. A zero id survives that filter, since it keys as the string "0".
 *
 * Colour is never the only carrier. The Team's name is written along its lane, so a reader who
 * cannot tell the hues apart loses the grouping shortcut and nothing else.
 */
function coloured(lanes: PendingLane[]): TeamLane[] {
	const colors = getColorMapForKeys(lanes.map((lane) => String(lane.teamId)));

	return lanes.map((lane) => ({
		featureId: lane.featureId,
		teamId: lane.teamId,
		teamName: lane.teamName,
		start: lane.start,
		end: lane.end,
		color: colors[String(lane.teamId)],
	}));
}
