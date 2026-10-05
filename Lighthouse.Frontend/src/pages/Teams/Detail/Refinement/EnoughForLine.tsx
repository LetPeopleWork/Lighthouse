import { Box, Typography } from "@mui/material";
import {
	GridRow,
	type GridRowProps,
	type GridRowSpacing,
	type GridRowSpacingParams,
} from "@mui/x-data-grid";
import type React from "react";
import { createContext, forwardRef, useContext, useMemo } from "react";
import {
	describeEnoughFor,
	type EnoughForTerms,
	isNumbered,
	type LineSide,
	lineBeside,
} from "./enoughForPlacement";
import type { ShownVerdict } from "./NeedVerdict";

/** How many rows are needed before the next Refinement, and what the line says about them. */
export interface EnoughForMarking {
	high: number;
	highPercentile: number;
	terms: EnoughForTerms;
}

/** Marks the rows only while the verdict is shown, so the list never counts up to a number the tab does not state. */
export const markEnoughFor = (
	verdict: ShownVerdict | null,
	terms: EnoughForTerms,
): EnoughForMarking | null =>
	verdict === null
		? null
		: {
				high: verdict.need.high,
				highPercentile: verdict.need.highPercentile,
				terms,
			};

export const EnoughForContext = createContext<EnoughForMarking | null>(null);

const ShownIndexContext = createContext(-1);

const LINE_HEIGHT_PX = 36;

const BELOW_THE_LINE_OPACITY = 0.6;

const SPACE_FOR_THE_LINE: Record<LineSide, GridRowSpacing> = {
	above: { top: LINE_HEIGHT_PX },
	below: { bottom: LINE_HEIGHT_PX },
};

/**
 * The grid lays its rows out from the heights it knows about, so the line lives in space the grid reserves
 * as row spacing; otherwise every row after it would be drawn a line lower than the grid thinks it is.
 * Both the space and the line are placed from the rows as shown, sorted and filtered, so they agree.
 */
const spaceForTheLine =
	(high: number) =>
	({
		indexRelativeToCurrentPage,
		isLastVisible,
	}: GridRowSpacingParams): GridRowSpacing => {
		const line = lineBeside(high, {
			index: indexRelativeToCurrentPage,
			isLastShown: isLastVisible,
		});
		return line === null ? {} : SPACE_FOR_THE_LINE[line.side];
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
		const beside = lineBeside(marking.high, {
			index: props.index,
			isLastShown: props.isLastVisible,
		});
		const line =
			beside === null ? null : (
				<Line
					side={beside.side}
					sentence={describeEnoughFor(beside.says, {
						shown: props.index + 1,
						highPercentile: marking.highPercentile,
						terms: marking.terms,
					})}
				/>
			);
		const style = isNumbered(marking.high, props.index)
			? props.style
			: { ...props.style, opacity: BELOW_THE_LINE_OPACITY };
		return (
			<ShownIndexContext.Provider value={props.index}>
				{beside?.side === "above" && line}
				<GridRow ref={ref} {...props} style={style} />
				{beside?.side === "below" && line}
			</ShownIndexContext.Provider>
		);
	},
);

const ROW_SLOTS = { row: EnoughForRow };

/** What the grid needs to draw the line: rows that can carry it, and the space to carry it in. */
export const useEnoughForLine = (marking: EnoughForMarking | null) => {
	const high = marking?.high;
	const getRowSpacing = useMemo(
		() => (high === undefined ? undefined : spaceForTheLine(high)),
		[high],
	);
	return { slots: ROW_SLOTS, getRowSpacing };
};

/** The row's place among the rows needed before the next Refinement, or nothing below the line. */
export const NeededNumber: React.FC = () => {
	const marking = useContext(EnoughForContext);
	const index = useContext(ShownIndexContext);
	if (marking === null || !isNumbered(marking.high, index)) {
		return null;
	}
	return index + 1;
};
