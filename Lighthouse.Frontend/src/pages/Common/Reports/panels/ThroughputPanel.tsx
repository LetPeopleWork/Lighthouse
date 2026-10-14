import { Typography } from "@mui/material";
import type React from "react";
import type {
	IReportValue,
	ReportMetricSide,
} from "../../../../models/Reports/Report";
import type { ReportPanelProps } from "./reportPanelRegistry";
import ThenNowPanelFrame, { PanelLine } from "./ThenNowPanelFrame";
import {
	absentReasonText,
	type ChangeVerdict,
	changeText,
	compareThenNow,
	type DirectionOfGood,
	type ReportSide,
	roundForDisplay,
} from "./thenNowComparison";

const NOT_CAPTURED = "Not captured for this report";
const DASH = "—";
const TOTAL_DECIMALS = 0;
const PER_DAY_DECIMALS = 2;

const verdictColours: Record<ChangeVerdict, string> = {
	better: "success.main",
	worse: "error.main",
	neutral: "text.secondary",
};

const shownValue = (
	value: IReportValue | null | undefined,
	decimals: number,
): number | null =>
	value?.value === null || value?.value === undefined
		? null
		: roundForDisplay(value.value, decimals);

const perDayText = (value: number) =>
	`${value.toFixed(PER_DAY_DECIMALS)} per day`;

interface SideProps {
	side: ReportMetricSide;
	sideName: ReportSide;
	workItems: string;
}

const reasonOf = (
	side: ReportMetricSide,
	sideName: ReportSide,
	workItems: string,
): string | null => {
	const absent = [side.total, side.perDay].filter(
		(value) => value?.value === null || value?.value === undefined,
	);
	if (absent.length === 0) return null;
	const reason = absent.find((value) => value?.absentReason)?.absentReason;
	return reason ? absentReasonText(reason, sideName, workItems) : NOT_CAPTURED;
};

const ThroughputLines: React.FC<Readonly<SideProps>> = ({
	side,
	sideName,
	workItems,
}) => {
	const total = shownValue(side.total, TOTAL_DECIMALS);
	const perDay = shownValue(side.perDay, PER_DAY_DECIMALS);
	const reason = reasonOf(side, sideName, workItems);
	return (
		<>
			<PanelLine>
				<Typography component="span">
					{total === null ? `Total ${DASH}` : `Total ${total} ${workItems}`}
				</Typography>
			</PanelLine>
			<PanelLine>
				<Typography component="span" sx={{ fontWeight: "bold" }}>
					{perDay === null ? DASH : perDayText(perDay)}
				</Typography>
			</PanelLine>
			{reason !== null && (
				<Typography variant="body2" color="text.secondary">
					{reason}
				</Typography>
			)}
		</>
	);
};

const PerDayChange: React.FC<{
	readonly then: number | null;
	readonly now: number | null;
	readonly directionOfGood: DirectionOfGood;
}> = ({ then, now, directionOfGood }) => {
	if (then === null || now === null) {
		return <Typography component="span">{DASH}</Typography>;
	}
	const change = compareThenNow(then, now, directionOfGood, PER_DAY_DECIMALS);
	return (
		<Typography
			component="span"
			data-change={change.verdict}
			sx={{ color: verdictColours[change.verdict], fontWeight: "bold" }}
		>
			{changeText(change, perDayText(Math.abs(change.delta)))}
		</Typography>
	);
};

/** The Work Items finished in each window and their average per day, compared on the average. */
const ThroughputPanel: React.FC<Readonly<ReportPanelProps>> = ({
	title,
	metric,
	workItems,
	directionOfGood,
}) => (
	<ThenNowPanelFrame
		title={title}
		then={
			<ThroughputLines
				side={metric.atThen}
				sideName="Then"
				workItems={workItems}
			/>
		}
		now={
			<ThroughputLines
				side={metric.atNow}
				sideName="Now"
				workItems={workItems}
			/>
		}
		change={
			<>
				<PanelLine>{null}</PanelLine>
				<PanelLine>
					<PerDayChange
						then={shownValue(metric.atThen.perDay, PER_DAY_DECIMALS)}
						now={shownValue(metric.atNow.perDay, PER_DAY_DECIMALS)}
						directionOfGood={directionOfGood}
					/>
				</PanelLine>
			</>
		}
	/>
);

export default ThroughputPanel;
