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

const TOTAL_DECIMALS = 0;
const PER_DAY_DECIMALS = 2;

const perDayText = (value: number) =>
	`${value.toFixed(PER_DAY_DECIMALS)} per day`;

/** One note for the side however many of its values are dashes, preferring one the server gave a reason for. */
const absenceNotesOf = ({
	side,
	sideName,
	workItems,
}: ThenNowSideProps): string[] => {
	const absent = [side.total, side.perDay].filter(
		(value) => shownValue(value, PER_DAY_DECIMALS) === null,
	);
	if (absent.length === 0) return [];
	const explained = absent.find((value) => value?.absentReason) ?? absent[0];
	return [absenceTextOf(explained, sideName, workItems)];
};

const ThroughputLines: React.FC<
	Readonly<ThenNowSideProps & { workItem: string }>
> = ({ workItem, ...sideProps }) => {
	const { side, workItems } = sideProps;
	const total = shownValue(side.total, TOTAL_DECIMALS);
	const perDay = shownValue(side.perDay, PER_DAY_DECIMALS);
	return (
		<>
			<PanelLine>
				<Typography component="span">
					{total === null
						? `Total ${DASH}`
						: `Total ${total} ${total === 1 ? workItem : workItems}`}
				</Typography>
			</PanelLine>
			<PanelLine>
				<Typography component="span" sx={{ fontWeight: "bold" }}>
					{perDay === null ? DASH : perDayText(perDay)}
				</Typography>
			</PanelLine>
			<AbsenceNotes notes={absenceNotesOf(sideProps)} />
		</>
	);
};

/** The Work Items finished in each window and their average per day, compared on the average. */
const ThroughputPanel: React.FC<Readonly<ReportPanelProps>> = ({
	title,
	metric,
	workItem,
	workItems,
	directionOfGood,
}) => (
	<ThenNowPanelFrame
		title={title}
		then={
			<ThroughputLines
				side={metric.atThen}
				sideName="Then"
				workItem={workItem}
				workItems={workItems}
			/>
		}
		now={
			<ThroughputLines
				side={metric.atNow}
				sideName="Now"
				workItem={workItem}
				workItems={workItems}
			/>
		}
		change={
			<>
				<PanelLine>{null}</PanelLine>
				<PanelLine>
					<LineChange
						then={shownValue(metric.atThen.perDay, PER_DAY_DECIMALS)}
						now={shownValue(metric.atNow.perDay, PER_DAY_DECIMALS)}
						directionOfGood={directionOfGood}
						decimals={PER_DAY_DECIMALS}
						magnitudeText={perDayText}
					/>
				</PanelLine>
			</>
		}
	/>
);

export default ThroughputPanel;
