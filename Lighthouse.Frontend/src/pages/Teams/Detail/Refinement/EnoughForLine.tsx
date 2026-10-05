import { Box, Typography } from "@mui/material";
import {
	GridRow,
	type GridRowProps,
	type GridRowSpacing,
	type GridRowSpacingParams,
} from "@mui/x-data-grid";
import type React from "react";
import { createContext, forwardRef, useContext } from "react";
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

const LINE_HEIGHT = 36;

/**
 * The grid lays its rows out from the heights it knows about, so the line lives in space the grid reserves
 * as row spacing; otherwise every row after it would be drawn a line lower than the grid thinks it is.
 */
export const spaceForTheLine =
	(lineAfterRow: number) =>
	({ indexRelativeToCurrentPage }: GridRowSpacingParams): GridRowSpacing => {
		if (indexRelativeToCurrentPage === lineAfterRow) {
			return { bottom: LINE_HEIGHT };
		}
		if (lineAfterRow === -1 && indexRelativeToCurrentPage === 0) {
			return { top: LINE_HEIGHT };
		}
		return {};
	};

const Line: React.FC<Readonly<{ sentence: string; beforeRow: boolean }>> = ({
	sentence,
	beforeRow,
}) => (
	<Box
		sx={{
			height: LINE_HEIGHT,
			...(beforeRow
				? { marginBottom: `-${LINE_HEIGHT}px` }
				: { marginTop: `-${LINE_HEIGHT}px` }),
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
export const EnoughForRow = forwardRef<HTMLDivElement, GridRowProps>(
	function EnoughForRow(props, ref) {
		const marking = useContext(EnoughForContext);
		if (marking === null) {
			return <GridRow ref={ref} {...props} />;
		}
		const { numbered, lineAfterRow } = marking.placement;
		const isBelowTheLine = props.index >= numbered;
		const line = (
			<Line sentence={marking.sentence} beforeRow={lineAfterRow === -1} />
		);
		return (
			<ShownIndexContext.Provider value={props.index}>
				{lineAfterRow === -1 && props.index === 0 && line}
				<GridRow
					ref={ref}
					{...props}
					style={
						isBelowTheLine ? { ...props.style, opacity: 0.6 } : props.style
					}
				/>
				{props.index === lineAfterRow && line}
			</ShownIndexContext.Provider>
		);
	},
);

/** The row's place among the rows needed before the next Refinement, or nothing below the line. */
export const NeededNumber: React.FC = () => {
	const marking = useContext(EnoughForContext);
	const index = useContext(ShownIndexContext);
	const isNumbered =
		marking !== null && index >= 0 && index < marking.placement.numbered;
	return isNumbered ? index + 1 : null;
};
