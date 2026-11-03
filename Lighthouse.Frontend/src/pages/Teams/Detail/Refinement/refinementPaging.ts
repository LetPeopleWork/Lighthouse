import type { GridRowSpacing } from "@mui/x-data-grid";
import type { RowInWholeList } from "../../../../components/Common/DataGrid/types";
import {
	isNumbered,
	type LineSide,
	lineBeside,
	type ShownRow,
} from "./enoughForPlacement";

export const LINE_HEIGHT_PX = 36;

const SPACE_FOR_THE_LINE: Record<LineSide, GridRowSpacing> = {
	above: { top: LINE_HEIGHT_PX },
	below: { bottom: LINE_HEIGHT_PX },
};

/** The row at this place in the whole sorted and filtered list, whichever page it is drawn on. */
export const shownRowOf = (
	index: number,
	shownCount: number,
	isOutOfSight?: boolean,
): ShownRow => ({
	index,
	isLastShown: index === shownCount - 1,
	isOutOfSight,
});

/** How many rows are shown up to and including the one at this place. */
export const shownCountOf = (index: number): number => index + 1;

/** The number in the "#" column for the row at this place, or nothing when it is below the line or no line is drawn. */
export const neededNumberAt = (
	high: number | undefined,
	index: number,
): number | null =>
	high !== undefined && isNumbered(high, index) ? shownCountOf(index) : null;

/**
 * The grid lays its rows out from the heights it knows about, so the line lives in space the grid reserves
 * as row spacing; otherwise every row after it would be drawn a line lower than the grid thinks it is.
 * Both the space and the line are placed from the row's place in the whole list, sorted and filtered,
 * so they agree on every page.
 */
export const spaceKeptFor = (
	high: number,
	row: RowInWholeList,
): GridRowSpacing => {
	const line = lineBeside(high, row);
	return line === null ? {} : SPACE_FOR_THE_LINE[line.side];
};
