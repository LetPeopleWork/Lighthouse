import { Box, Typography } from "@mui/material";
import {
	GridRow,
	type GridRowProps,
	type GridRowSpacing,
	type GridRowSpacingParams,
} from "@mui/x-data-grid";
import type React from "react";
import { createContext, forwardRef, useContext, useMemo } from "react";
import type { IRefinementNeed } from "../../../../models/Refinement/Refinement";
import {
	describeEnoughFor,
	type EnoughForPlacement,
	type EnoughForTerms,
	placeEnoughForLine,
} from "./enoughForPlacement";
import { isJudged } from "./NeedVerdict";

/** Where the line goes among the rows as shown, and what it says. */
export interface EnoughForMarking {
	placement: EnoughForPlacement;
	sentence: string;
}

/** Marks the rows only when the server judged the need, because only then is there a number to count up to. */
export const markEnoughFor = (
	need: IRefinementNeed | undefined,
	listed: number,
	terms: EnoughForTerms,
): EnoughForMarking | null => {
	if (!isJudged(need)) {
		return null;
	}
	const placement = placeEnoughForLine(need.high, listed);
	return {
		placement,
		sentence: describeEnoughFor(placement.says, {
			listed,
			highPercentile: need.highPercentile,
			terms,
		}),
	};
};

export const EnoughForContext = createContext<EnoughForMarking | null>(null);

const ShownIndexContext = createContext(-1);

const LINE_HEIGHT_PX = 36;

const BELOW_THE_LINE_OPACITY = 0.6;

type LineSide = "above" | "below";

/** The side of the shown row the line sits on, or null when the line is not next to that row. */
const lineBeside = (lineAfterRow: number, index: number): LineSide | null => {
	if (index === lineAfterRow) {
		return "below";
	}
	if (lineAfterRow === -1 && index === 0) {
		return "above";
	}
	return null;
};

const isNumberedRow = (placement: EnoughForPlacement, index: number) =>
	index >= 0 && index < placement.numbered;

const SPACE_FOR_THE_LINE: Record<LineSide, GridRowSpacing> = {
	above: { top: LINE_HEIGHT_PX },
	below: { bottom: LINE_HEIGHT_PX },
};

/**
 * The grid lays its rows out from the heights it knows about, so the line lives in space the grid reserves
 * as row spacing; otherwise every row after it would be drawn a line lower than the grid thinks it is.
 */
const spaceForTheLine =
	(lineAfterRow: number) =>
	({ indexRelativeToCurrentPage }: GridRowSpacingParams): GridRowSpacing => {
		const side = lineBeside(lineAfterRow, indexRelativeToCurrentPage);
		return side === null ? {} : SPACE_FOR_THE_LINE[side];
	};

/** The line pulls itself back into the space reserved for it rather than pushing the next row down. */
const INTO_THE_RESERVED_SPACE: Record<
	LineSide,
	{ marginTop: string } | { marginBottom: string }
> = {
	above: { marginBottom: `-${LINE_HEIGHT_PX}px` },
	below: { marginTop: `-${LINE_HEIGHT_PX}px` },
};

const Line: React.FC<Readonly<{ sentence: string; side: LineSide }>> = ({
	sentence,
	side,
}) => (
	<Box
		sx={{
			height: LINE_HEIGHT_PX,
			...INTO_THE_RESERVED_SPACE[side],
			display: "flex",
			alignItems: "center",
			px: 2,
			borderTop: 2,
			borderColor: "divider",
			overflow: "hidden",
		}}
	>
		<Typography variant="body2" color="text.secondary" noWrap>
			{sentence}
		</Typography>
	</Box>
);

/** A grid row that knows where it is shown, with the line before or after it when that is where the line goes. */
const EnoughForRow = forwardRef<HTMLDivElement, GridRowProps>(
	function EnoughForRow(props, ref) {
		const marking = useContext(EnoughForContext);
		if (marking === null) {
			return <GridRow ref={ref} {...props} />;
		}
		const side = lineBeside(marking.placement.lineAfterRow, props.index);
		const line =
			side === null ? null : <Line sentence={marking.sentence} side={side} />;
		const style = isNumberedRow(marking.placement, props.index)
			? props.style
			: { ...props.style, opacity: BELOW_THE_LINE_OPACITY };
		return (
			<ShownIndexContext.Provider value={props.index}>
				{side === "above" && line}
				<GridRow ref={ref} {...props} style={style} />
				{side === "below" && line}
			</ShownIndexContext.Provider>
		);
	},
);

const ROW_SLOTS = { row: EnoughForRow };

/** What the grid needs to draw the line: rows that can carry it, and the space to carry it in. */
export const useEnoughForLine = (marking: EnoughForMarking | null) => {
	const lineAfterRow = marking?.placement.lineAfterRow;
	const getRowSpacing = useMemo(
		() =>
			lineAfterRow === undefined ? undefined : spaceForTheLine(lineAfterRow),
		[lineAfterRow],
	);
	return { slots: ROW_SLOTS, getRowSpacing };
};

/** The row's place among the rows needed before the next Refinement, or nothing below the line. */
export const NeededNumber: React.FC = () => {
	const marking = useContext(EnoughForContext);
	const index = useContext(ShownIndexContext);
	if (marking === null || !isNumberedRow(marking.placement, index)) {
		return null;
	}
	return index + 1;
};
