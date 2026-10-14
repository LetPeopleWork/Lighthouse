import { Typography } from "@mui/material";
import type React from "react";
import { ForecastLevel } from "../../../../components/Common/Forecasts/ForecastLevel";
import type { ReportPanelProps } from "./reportPanelRegistry";
import {
	AbsenceNotes,
	LineChange,
	type ThenNowSideProps,
} from "./ThenNowLineParts";
import ThenNowPanelFrame, { PanelLine } from "./ThenNowPanelFrame";
import { absenceTextOf, DASH, shownValue } from "./thenNowComparison";

const DECIMALS = 0;

const PERCENTILES = [
	{ key: "p85", label: "85th", percentile: 85 },
	{ key: "p70", label: "70th", percentile: 70 },
	{ key: "p50", label: "50th", percentile: 50 },
] as const;

const dayWord = (value: number) => (Math.abs(value) === 1 ? "day" : "days");

const daysText = (value: number) => `${value} ${dayWord(value)}`;

/** Each distinct reason once, so three dashes for the same cause do not say it three times. */
const absenceNotesOf = ({
	side,
	sideName,
	workItems,
}: ThenNowSideProps): string[] => {
	const notes = PERCENTILES.filter(
		({ key }) => shownValue(side[key], DECIMALS) === null,
	).map(({ key }) => absenceTextOf(side[key], sideName, workItems));
	return [...new Set(notes)];
};

const PercentileLines: React.FC<Readonly<ThenNowSideProps>> = (sideProps) => (
	<>
		{PERCENTILES.map(({ key, label, percentile }) => {
			const level = new ForecastLevel(percentile);
			const Icon = level.IconComponent;
			const shown = shownValue(sideProps.side[key], DECIMALS);
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
		<AbsenceNotes notes={absenceNotesOf(sideProps)} />
	</>
);

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
				<LineChange
					then={shownValue(metric.atThen[key], DECIMALS)}
					now={shownValue(metric.atNow[key], DECIMALS)}
					directionOfGood={directionOfGood}
					decimals={DECIMALS}
					magnitudeText={daysText}
				/>
			</PanelLine>
		))}
	/>
);

export default CycleTimePanel;
