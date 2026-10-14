import { Typography } from "@mui/material";
import type React from "react";
import type {
	IReportMetric,
	ReportMetricSide,
} from "../../../../models/Reports/Report";
import { PanelLine } from "./ThenNowPanelFrame";
import {
	type ChangeLine,
	type ChangeVerdict,
	changeText,
	compareThenNow,
	DASH,
	type DirectionOfGood,
	type ReportSide,
	shownValue,
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
const LineChange: React.FC<{
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

/** A panel's change column: a change on each of its change lines, in their order. */
export const LineChanges: React.FC<{
	readonly metric: IReportMetric;
	readonly lines: readonly ChangeLine[];
	readonly directionOfGood: DirectionOfGood;
	readonly magnitudeText: (magnitude: number) => string;
}> = ({ metric, lines, directionOfGood, magnitudeText }) =>
	lines.map(({ key, decimals }) => (
		<PanelLine key={key}>
			<LineChange
				then={shownValue(metric.atThen[key], decimals)}
				now={shownValue(metric.atNow[key], decimals)}
				directionOfGood={directionOfGood}
				decimals={decimals}
				magnitudeText={magnitudeText}
			/>
		</PanelLine>
	));

/** A label and its value in bold, led by an icon and coloured where the panel gives one. */
export const ValueLine: React.FC<{
	readonly label: string;
	readonly value: string;
	readonly icon?: React.ReactNode;
	readonly color?: string;
}> = ({ label, value, icon, color }) => (
	<PanelLine>
		{icon}
		<Typography component="span">
			<span>{label}</span>{" "}
			<Typography component="span" sx={{ fontWeight: "bold", color }}>
				{value}
			</Typography>
		</Typography>
	</PanelLine>
);

/** Below a side's lines, why its dashes are dashes. */
export const AbsenceNotes: React.FC<{ readonly notes: readonly string[] }> = ({
	notes,
}) =>
	notes.map((note) => (
		<Typography key={note} variant="body2" color="text.secondary">
			{note}
		</Typography>
	));
