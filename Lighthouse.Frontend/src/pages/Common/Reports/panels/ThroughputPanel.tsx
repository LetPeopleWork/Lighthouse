import CalendarTodayIcon from "@mui/icons-material/CalendarToday";
import FunctionsIcon from "@mui/icons-material/Functions";
import { Typography } from "@mui/material";
import type React from "react";
import type { ReportPanelProps } from "./reportPanelRegistry";
import {
	AbsenceNotes,
	LineChanges,
	NeutralLineIcon,
	type ThenNowSideProps,
} from "./ThenNowLineParts";
import ThenNowPanelFrame, { PanelLine } from "./ThenNowPanelFrame";
import {
	absenceTextOf,
	DASH,
	shownText,
	shownValue,
} from "./thenNowComparison";

const TOTAL_DECIMALS = 0;
const PER_DAY_DECIMALS = 2;

export const CHANGE_LINES = [
	{ key: "perDay", label: "per day", decimals: PER_DAY_DECIMALS },
] as const;

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

const totalText = (
	total: number | null,
	workItem: string,
	workItems: string,
): string => {
	if (total === null) return `Total ${DASH}`;
	const noun = total === 1 ? workItem : workItems;
	return `Total ${total} ${noun}`;
};

const ThroughputLines: React.FC<
	Readonly<ThenNowSideProps & { workItem: string }>
> = ({ workItem, ...sideProps }) => {
	const { side, workItems } = sideProps;
	const total = shownValue(side.total, TOTAL_DECIMALS);
	return (
		<>
			<PanelLine>
				<NeutralLineIcon Icon={FunctionsIcon} />
				<Typography component="span">
					{totalText(total, workItem, workItems)}
				</Typography>
			</PanelLine>
			<PanelLine>
				<NeutralLineIcon Icon={CalendarTodayIcon} />
				<Typography component="span" sx={{ fontWeight: "bold" }}>
					{shownText(side.perDay, PER_DAY_DECIMALS, perDayText)}
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
				<LineChanges
					metric={metric}
					lines={CHANGE_LINES}
					directionOfGood={directionOfGood}
					magnitudeText={perDayText}
				/>
			</>
		}
	/>
);

export default ThroughputPanel;
