import { Typography } from "@mui/material";
import type React from "react";
import type { ReportMetricSide } from "../../../../models/Reports/Report";
import {
	type ChangeVerdict,
	changeText,
	compareThenNow,
	DASH,
	type DirectionOfGood,
	type ReportSide,
} from "./thenNowComparison";

/** One side of a panel: its values, which side it is, and the instance's word for Work Items. */
export interface ThenNowSideProps {
	side: ReportMetricSide;
	sideName: ReportSide;
	workItems: string;
}

const verdictColours: Record<ChangeVerdict, string> = {
	better: "success.main",
	worse: "error.main",
	neutral: "text.secondary",
};

/** The change on one line, told by sign and arrow as well as colour; a dash when either side is one. */
export const LineChange: React.FC<{
	readonly then: number | null;
	readonly now: number | null;
	readonly directionOfGood: DirectionOfGood;
	readonly decimals: number;
	readonly magnitudeText: (magnitude: number) => string;
}> = ({ then, now, directionOfGood, decimals, magnitudeText }) => {
	if (then === null || now === null) {
		return <Typography component="span">{DASH}</Typography>;
	}
	const change = compareThenNow(then, now, directionOfGood, decimals);
	return (
		<Typography
			component="span"
			data-change={change.verdict}
			sx={{ color: verdictColours[change.verdict], fontWeight: "bold" }}
		>
			{changeText(change, magnitudeText(Math.abs(change.delta)))}
		</Typography>
	);
};

/** Below a side's lines, why its dashes are dashes. */
export const AbsenceNotes: React.FC<{ readonly notes: readonly string[] }> = ({
	notes,
}) =>
	notes.map((note) => (
		<Typography key={note} variant="body2" color="text.secondary">
			{note}
		</Typography>
	));
