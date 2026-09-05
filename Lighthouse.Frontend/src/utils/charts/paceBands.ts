import type { IPercentileValue } from "../../models/PercentileValue";
import type { IPerStatePercentileValues } from "../../models/PerStatePercentileValues";
import type { IWorkItem } from "../../models/WorkItem";
import {
	certainColor,
	confidentColor,
	errorColor,
	realisticColor,
} from "../theme/colors";

/**
 * Two surfaces answer the same question about an in-flight item: the aging chart paints a coloured
 * zone behind its dot, and the work item dialog names the band in a cell. They must never contradict
 * each other, so the rule that decides the band lives here once and both of them read it. Nothing in
 * this file knows about React, about a chart's coordinates, or about a grid.
 */

export const PACE_BAND_COLORS_LOW_TO_HIGH = [
	certainColor,
	confidentColor,
	"#fbc02d",
	realisticColor,
	errorColor,
] as const;

/**
 * What an item reads when its state has no completed history to be measured against. It is
 * deliberately neither blank nor green: not knowing is not the same as doing well.
 */
export const NO_HISTORY_BAND_LABEL = "No history";

/** One state's percentiles, value-ascending, after a state with none of its own has inherited them. */
export interface PaceBandLadder {
	readonly state: string;
	readonly percentiles: readonly IPercentileValue[];
}

/** Ladders by position in the workflow. A state with nothing to measure against is absent. */
export type PaceBandLadders = ReadonlyMap<number, PaceBandLadder>;

export interface PaceBandLadderInputs {
	readonly perStatePercentileValues: IPerStatePercentileValues[];
	readonly doingStates: string[];
}

/**
 * Walks the workflow in order and gives every state a ladder, letting a state with no history of its
 * own inherit the one before it. States that come before any history at all get nothing.
 */
export const resolvePaceBandLadders = ({
	perStatePercentileValues,
	doingStates,
}: PaceBandLadderInputs): PaceBandLadders => {
	const percentilesByState = new Map<string, readonly IPercentileValue[]>();
	for (const perState of perStatePercentileValues) {
		if (perState.percentiles.length > 0) {
			percentilesByState.set(
				perState.state.toLowerCase(),
				[...perState.percentiles].sort((a, b) => a.value - b.value),
			);
		}
	}

	const ladders = new Map<number, PaceBandLadder>();
	let carried: readonly IPercentileValue[] | undefined;

	doingStates.forEach((state, position) => {
		carried = percentilesByState.get(state.toLowerCase()) ?? carried;
		if (!carried) {
			return;
		}

		ladders.set(position, { state, percentiles: carried });
	});

	return ladders;
};

/**
 * The rank of the first boundary the age does not exceed, or the top rank when it exceeds them all.
 * Undefined when the state has no ladder. An age sitting exactly on a boundary belongs to the band
 * beneath it, which is the band the chart paints at that height.
 */
export const classifyPaceBand = (
	ageInDays: number,
	stateName: string,
	ladders: PaceBandLadders,
): number | undefined => {
	const wantedState = stateName.toLowerCase();

	for (const ladder of ladders.values()) {
		if (ladder.state.toLowerCase() === wantedState) {
			const rank = ladder.percentiles.findIndex(
				(percentile) => ageInDays <= percentile.value,
			);

			return rank === -1 ? ladder.percentiles.length : rank;
		}
	}

	return undefined;
};

/**
 * The fill for a rank. Short ladders still paint their top band the reddest colour, and past the
 * fifth rank the palette stops distinguishing — the label carries what the colour no longer can.
 */
export const paceBandColorForRank = (
	_rank: number,
	_boundaryCount: number,
): string => {
	throw new Error("paceBandColorForRank has no implementation yet");
};

/**
 * Reads the band's name off the percentile numbers themselves, so a ladder of a different length
 * still names its top band honestly instead of borrowing a middle band's wording.
 */
export const paceBandLabelForRank = (
	_rank: number | undefined,
	_ladder: PaceBandLadder | undefined,
): string => {
	throw new Error("paceBandLabelForRank has no implementation yet");
};

/**
 * Every band name a team can show, worst last, with the no-history sentinel first so that ordering by
 * this list puts an unknown below the lowest band rather than at the head of a worst-first list.
 */
export const paceBandOptionLabels = (_ladders: PaceBandLadders): string[] => {
	throw new Error("paceBandOptionLabels has no implementation yet");
};

/**
 * Everything the work item dialog needs to draw the band column, and nothing about where the bands
 * came from. The dialog is shared by many callers and stays ignorant of percentiles.
 */
export interface AgeBandColumnDescriptor {
	readonly headerName: string;
	readonly description: string;
	readonly optionLabels: string[];
	readonly bandFor: (workItem: IWorkItem) => string;
	readonly colorForBand: (label: string) => string | undefined;
}

export interface AgeBandColumnInputs extends PaceBandLadderInputs {
	readonly headerName: string;
	readonly description: string;
}

/**
 * Builds the descriptor, or returns nothing when no state has any history — a column that read
 * "No history" on every row would be noise, and the chart draws no zones in that case either.
 */
export const buildAgeBandColumnDescriptor = (
	_inputs: AgeBandColumnInputs,
): AgeBandColumnDescriptor | undefined => {
	throw new Error("buildAgeBandColumnDescriptor has no implementation yet");
};
