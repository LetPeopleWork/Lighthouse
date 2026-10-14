import type { SvgIconComponent } from "@mui/icons-material";
import { Box, Typography } from "@mui/material";
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
	DASH,
	type DirectionOfGood,
	lineComparisonsOf,
	NO_CHANGE_MARK,
	type ReportSide,
	type ThenNowChange,
} from "./thenNowComparison";

/** One side of a panel: its values, which side it is, and the instance's word for Work Items. */
export interface ThenNowSideProps {
	side: ReportMetricSide;
	sideName: ReportSide;
	workItems: string;
}

/** Only a change's mark and words are coloured, green for better and red for worse. */
export const verdictColours: Record<ChangeVerdict, string> = {
	better: "success.main",
	worse: "error.main",
	neutral: "text.secondary",
};

/** Off screen but read aloud. */
export const visuallyHidden = {
	position: "absolute",
	width: 1,
	height: 1,
	margin: "-1px",
	padding: 0,
	border: 0,
	overflow: "hidden",
	clipPath: "inset(50%)",
	whiteSpace: "nowrap",
} as const;

/**
 * Names the line a change belongs to for a screen reader. It reads the change column after the labels, not beside
 * them, so without a name it hears a bare "No change" with nothing to say which line did not change.
 */
const LineName: React.FC<{ readonly label: string }> = ({ label }) => (
	<Box component="span" sx={visuallyHidden}>{`${label}: `}</Box>
);

/** A slot of its own for the arrow, or the dash of no change, so the marks line up down the column. */
const ChangeMark: React.FC<{ readonly mark: ThenNowChange["mark"] }> = ({
	mark,
}) => (
	<Box
		component="span"
		aria-hidden={mark === NO_CHANGE_MARK ? true : undefined}
		sx={{ display: "inline-block", minWidth: "1em", textAlign: "center" }}
	>
		{mark}
	</Box>
);

/** A value line's icon, in grey: colour is kept for telling better from worse. */
export const NeutralLineIcon: React.FC<{ readonly Icon: SvgIconComponent }> = ({
	Icon,
}) => <Icon fontSize="small" sx={{ color: "text.secondary" }} />;

/** The change on one line, told by sign and arrow as well as colour; a dash when either side is one. */
const LineChange: React.FC<{
	readonly change: ThenNowChange | null;
	readonly magnitudeText: (magnitude: number) => string;
}> = ({ change, magnitudeText }) => {
	if (change === null) {
		return <Typography component="span">{DASH}</Typography>;
	}
	return (
		<Typography
			component="span"
			data-change={change.verdict}
			sx={{ color: verdictColours[change.verdict], fontWeight: "bold" }}
		>
			<ChangeMark mark={change.mark} />{" "}
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
}> = ({ metric, lines, directionOfGood, magnitudeText }) => {
	const comparisons = lineComparisonsOf(metric, {
		changeLines: lines,
		directionOfGood,
	});
	return lines.map(({ key, label }, index) => (
		<PanelLine key={key}>
			<LineName label={label} />
			<LineChange
				change={comparisons[index].change}
				magnitudeText={magnitudeText}
			/>
		</PanelLine>
	));
};

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
