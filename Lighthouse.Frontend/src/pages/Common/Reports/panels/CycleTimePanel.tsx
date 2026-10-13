import { Box, Typography } from "@mui/material";
import type React from "react";
import type { IReportValue } from "../../../../models/Reports/Report";
import type { ReportPanelProps } from "./reportPanelRegistry";
import ThenNowPanelFrame from "./ThenNowPanelFrame";
import {
	absentReasonText,
	type ChangeVerdict,
	compareThenNow,
	type DirectionOfGood,
	type ReportSide,
	roundForDisplay,
	signed,
} from "./thenNowComparison";

const NOT_CAPTURED = "Not captured for this report";
const DASH = "—";
const DECIMALS = 0;

const verdictColours: Record<ChangeVerdict, string> = {
	better: "success.main",
	worse: "error.main",
	neutral: "text.secondary",
};

const dayWord = (value: number) => (Math.abs(value) === 1 ? "day" : "days");

const shownValue = (value: IReportValue | null | undefined): number | null =>
	value?.value === null || value?.value === undefined
		? null
		: roundForDisplay(value.value, DECIMALS);

interface PercentileLineProps {
	label: string;
	value: IReportValue | null | undefined;
	side: ReportSide;
	workItems: string;
	withSampleSize: boolean;
	children?: React.ReactNode;
}

const PercentileLine: React.FC<Readonly<PercentileLineProps>> = ({
	label,
	value,
	side,
	workItems,
	withSampleSize,
	children,
}) => {
	const shown = shownValue(value);
	const sampleSize =
		withSampleSize &&
		value?.sampleSize !== null &&
		value?.sampleSize !== undefined
			? ` · ${value.sampleSize} ${workItems}`
			: "";
	const reason = value?.absentReason
		? absentReasonText(value.absentReason, side, workItems)
		: NOT_CAPTURED;
	return (
		<Box>
			<Box sx={{ display: "flex", flexWrap: "wrap", columnGap: 3 }}>
				<Typography component="span">
					{shown === null
						? `${label} ${DASH}`
						: `${label} ${shown} ${dayWord(shown)}${sampleSize}`}
				</Typography>
				{children}
			</Box>
			{shown === null && (
				<Typography variant="body2" color="text.secondary">
					{reason}
				</Typography>
			)}
		</Box>
	);
};

const Change: React.FC<{
	readonly then: number | null;
	readonly now: number | null;
	readonly directionOfGood: DirectionOfGood;
}> = ({ then, now, directionOfGood }) => {
	if (then === null || now === null) {
		return <Typography component="span">{DASH}</Typography>;
	}
	const { delta, percent, arrow, verdict } = compareThenNow(
		then,
		now,
		directionOfGood,
		DECIMALS,
	);
	const percentText = percent === null ? "Then was 0" : `${signed(percent)}%`;
	return (
		<Typography
			component="span"
			data-change={verdict}
			sx={{ color: verdictColours[verdict], fontWeight: "bold" }}
		>
			{`${signed(delta)} ${dayWord(delta)} ${percentText} ${arrow}`.trim()}
		</Typography>
	);
};

/** Then's 85th percentile against Now's 85th, 70th and 50th, the change told on the 85th. */
const CycleTimePanel: React.FC<Readonly<ReportPanelProps>> = ({
	title,
	metric,
	workItems,
	directionOfGood,
}) => (
	<ThenNowPanelFrame
		title={title}
		then={
			<PercentileLine
				label="85th"
				value={metric.atThen.p85}
				side="Then"
				workItems={workItems}
				withSampleSize
			/>
		}
		now={
			<>
				<PercentileLine
					label="85th"
					value={metric.atNow.p85}
					side="Now"
					workItems={workItems}
					withSampleSize
				>
					<Change
						then={shownValue(metric.atThen.p85)}
						now={shownValue(metric.atNow.p85)}
						directionOfGood={directionOfGood}
					/>
				</PercentileLine>
				<PercentileLine
					label="70th"
					value={metric.atNow.p70}
					side="Now"
					workItems={workItems}
					withSampleSize={false}
				/>
				<PercentileLine
					label="50th"
					value={metric.atNow.p50}
					side="Now"
					workItems={workItems}
					withSampleSize={false}
				/>
			</>
		}
	/>
);

export default CycleTimePanel;
