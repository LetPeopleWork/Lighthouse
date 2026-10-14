import { Typography } from "@mui/material";
import type React from "react";
import type { ReportPanelProps } from "./reportPanelRegistry";
import {
	AbsenceNotes,
	LineChange,
	type ThenNowSideProps,
} from "./ThenNowLineParts";
import ThenNowPanelFrame, { PanelLine } from "./ThenNowPanelFrame";
import { absenceTextOf, DASH, shownValue } from "./thenNowComparison";

const DECIMALS = 0;

const LINES = [
	{ key: "average", label: "Average" },
	{ key: "median", label: "Median" },
	{ key: "min", label: "Min" },
	{ key: "max", label: "Max" },
] as const;

type ValueText = (value: number) => string;

/** Each distinct reason once, so four dashes for the same cause do not say it four times. */
const absenceNotesOf = ({
	side,
	sideName,
	workItems,
}: ThenNowSideProps): string[] => {
	const notes = LINES.filter(
		({ key }) => shownValue(side[key], DECIMALS) === null,
	).map(({ key }) => absenceTextOf(side[key], sideName, workItems));
	return [...new Set(notes)];
};

const SpreadLines: React.FC<
	Readonly<ThenNowSideProps & { valueText: ValueText }>
> = ({ valueText, ...sideProps }) => (
	<>
		{LINES.map(({ key, label }) => {
			const shown = shownValue(sideProps.side[key], DECIMALS);
			return (
				<PanelLine key={key}>
					<Typography component="span">
						<span>{label}</span>{" "}
						<Typography component="span" sx={{ fontWeight: "bold" }}>
							{shown === null ? DASH : valueText(shown)}
						</Typography>
					</Typography>
				</PanelLine>
			);
		})}
		<AbsenceNotes notes={absenceNotesOf(sideProps)} />
	</>
);

/** The average, median, min and max of a window's daily values, Then beside Now, with the change on each. */
const DailySpreadPanel: React.FC<
	Readonly<ReportPanelProps & { valueText: ValueText }>
> = ({ title, metric, workItems, directionOfGood, valueText }) => (
	<ThenNowPanelFrame
		title={title}
		then={
			<SpreadLines
				side={metric.atThen}
				sideName="Then"
				workItems={workItems}
				valueText={valueText}
			/>
		}
		now={
			<SpreadLines
				side={metric.atNow}
				sideName="Now"
				workItems={workItems}
				valueText={valueText}
			/>
		}
		change={LINES.map(({ key }) => (
			<PanelLine key={key}>
				<LineChange
					then={shownValue(metric.atThen[key], DECIMALS)}
					now={shownValue(metric.atNow[key], DECIMALS)}
					directionOfGood={directionOfGood}
					decimals={DECIMALS}
					magnitudeText={valueText}
				/>
			</PanelLine>
		))}
	/>
);

export default DailySpreadPanel;
