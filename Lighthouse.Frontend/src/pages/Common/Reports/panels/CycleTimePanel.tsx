import { Typography } from "@mui/material";
import type React from "react";
import { ForecastLevel } from "../../../../components/Common/Forecasts/ForecastLevel";
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
const DECIMALS = 0;

const PERCENTILES = [
	{ key: "p85", label: "85th", percentile: 85 },
	{ key: "p70", label: "70th", percentile: 70 },
	{ key: "p50", label: "50th", percentile: 50 },
] as const;

const verdictColours: Record<ChangeVerdict, string> = {
	better: "success.main",
	worse: "error.main",
	neutral: "text.secondary",
};

const dayWord = (value: number) => (Math.abs(value) === 1 ? "day" : "days");

const daysText = (value: number) => `${value} ${dayWord(value)}`;

const shownValue = (value: IReportValue | null | undefined): number | null =>
	value?.value === null || value?.value === undefined
		? null
		: roundForDisplay(value.value, DECIMALS);

/** Each distinct reason once, below the lines, so three dashes for the same cause do not say it three times. */
const reasonsOf = (
	side: ReportMetricSide,
	sideName: ReportSide,
	workItems: string,
): string[] => {
	const reasons = PERCENTILES.filter(
		({ key }) => shownValue(side[key]) === null,
	).map(({ key }) => {
		const reason = side[key]?.absentReason;
		return reason
			? absentReasonText(reason, sideName, workItems)
			: NOT_CAPTURED;
	});
	return [...new Set(reasons)];
};

interface SideProps {
	side: ReportMetricSide;
	sideName: ReportSide;
	workItems: string;
}

const PercentileLines: React.FC<Readonly<SideProps>> = ({
	side,
	sideName,
	workItems,
}) => (
	<>
		{PERCENTILES.map(({ key, label, percentile }) => {
			const level = new ForecastLevel(percentile);
			const Icon = level.IconComponent;
			const shown = shownValue(side[key]);
			return (
				<PanelLine key={key}>
					<Icon fontSize="small" sx={{ color: level.color }} />
					<Typography component="span">
						<span>{label}</span>{" "}
						<Typography
							component="span"
							sx={{ fontWeight: "bold", color: level.color }}
						>
							{shown === null ? DASH : daysText(shown)}
						</Typography>
					</Typography>
				</PanelLine>
			);
		})}
		{reasonsOf(side, sideName, workItems).map((reason) => (
			<Typography key={reason} variant="body2" color="text.secondary">
				{reason}
			</Typography>
		))}
	</>
);

const Change: React.FC<{
	readonly then: number | null;
	readonly now: number | null;
	readonly directionOfGood: DirectionOfGood;
}> = ({ then, now, directionOfGood }) => {
	if (then === null || now === null) {
		return <Typography component="span">{DASH}</Typography>;
	}
	const change = compareThenNow(then, now, directionOfGood, DECIMALS);
	return (
		<Typography
			component="span"
			data-change={change.verdict}
			sx={{ color: verdictColours[change.verdict], fontWeight: "bold" }}
		>
			{changeText(change, daysText(Math.abs(change.delta)))}
		</Typography>
	);
};

/** Then's 85th, 70th and 50th percentiles beside Now's, with the change on each. */
const CycleTimePanel: React.FC<Readonly<ReportPanelProps>> = ({
	title,
	metric,
	workItems,
	directionOfGood,
}) => (
	<ThenNowPanelFrame
		title={title}
		then={
			<PercentileLines
				side={metric.atThen}
				sideName="Then"
				workItems={workItems}
			/>
		}
		now={
			<PercentileLines
				side={metric.atNow}
				sideName="Now"
				workItems={workItems}
			/>
		}
		change={PERCENTILES.map(({ key }) => (
			<PanelLine key={key}>
				<Change
					then={shownValue(metric.atThen[key])}
					now={shownValue(metric.atNow[key])}
					directionOfGood={directionOfGood}
				/>
			</PanelLine>
		))}
	/>
);

export default CycleTimePanel;
