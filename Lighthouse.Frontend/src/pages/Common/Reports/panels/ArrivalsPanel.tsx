import CalendarTodayIcon from "@mui/icons-material/CalendarToday";
import FunctionsIcon from "@mui/icons-material/Functions";
import { Typography } from "@mui/material";
import type React from "react";
import InfoPopoverButton from "../../../../components/Common/InfoPopoverButton/InfoPopoverButton";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import type { ReportPanelProps } from "./reportPanelRegistry";
import {
	AbsenceNotes,
	LineChanges,
	NeutralLineIcon,
	type ThenNowSideProps,
	ValueLine,
} from "./ThenNowLineParts";
import ThenNowPanelFrame, { PanelLine } from "./ThenNowPanelFrame";
import { PER_DAY_DECIMALS, TOTAL_DECIMALS } from "./ThroughputPanel";
import {
	absenceTextOf,
	DASH,
	shownText,
	shownValue,
} from "./thenNowComparison";

/** The rows above the gap sentence, so the change mark sits level with it. */
const ROWS_ABOVE_THE_GAP = 3;

export const ARRIVALS_CHANGE_LINES = [
	{
		key: "gapPerDay",
		label: "gap per day",
		decimals: PER_DAY_DECIMALS,
		showPercent: false,
	},
] as const;

export type GapSentence =
	| { readonly kind: "more"; readonly magnitude: number }
	| { readonly kind: "fewer"; readonly magnitude: number }
	| { readonly kind: "balanced"; readonly magnitude: 0 };

/** What a gap, already rounded as shown, says: more or fewer started than finished, or as many. */
export const gapSentenceOf = (rounded: number): GapSentence => {
	if (rounded > 0) return { kind: "more", magnitude: rounded };
	if (rounded < 0) return { kind: "fewer", magnitude: Math.abs(rounded) };
	return { kind: "balanced", magnitude: 0 };
};

const gapText = (rounded: number): string => {
	const sentence = gapSentenceOf(rounded);
	const amount = sentence.magnitude.toFixed(PER_DAY_DECIMALS);
	switch (sentence.kind) {
		case "more":
			return `${amount} more started than finished per day`;
		case "fewer":
			return `${amount} fewer started than finished per day`;
		case "balanced":
			return "as many started as finished per day";
	}
};

const perDayText = (value: number) =>
	`${value.toFixed(PER_DAY_DECIMALS)} per day`;

const totalText = (total: number | null, workItems: string): string => {
	if (total === null) return `Total started ${DASH}`;
	return `Total started ${total} ${workItems}`;
};

const MEMBERS = ["total", "perDay", "throughputPerDay", "gapPerDay"] as const;

/** One note for the side however many of its values are dashes, preferring one the server gave a reason for. */
const absenceNotesOf = ({
	side,
	sideName,
	workItems,
}: ThenNowSideProps): string[] => {
	const absent = MEMBERS.map((member) => side[member]).filter(
		(value) => shownValue(value, PER_DAY_DECIMALS) === null,
	);
	if (absent.length === 0) return [];
	const explained = absent.find((value) => value?.absentReason) ?? absent[0];
	return [absenceTextOf(explained, sideName, workItems)];
};

const ArrivalsLines: React.FC<Readonly<ThenNowSideProps>> = (sideProps) => {
	const { side, workItems } = sideProps;
	const { getTerm } = useTerminology();
	const gap = shownValue(side.gapPerDay, PER_DAY_DECIMALS);
	return (
		<>
			<PanelLine>
				<NeutralLineIcon Icon={FunctionsIcon} />
				<Typography component="span">
					{totalText(shownValue(side.total, TOTAL_DECIMALS), workItems)}
				</Typography>
			</PanelLine>
			<ValueLine
				icon={<NeutralLineIcon Icon={CalendarTodayIcon} />}
				label="Arrivals"
				value={shownText(side.perDay, PER_DAY_DECIMALS, perDayText)}
			/>
			<ValueLine
				icon={<NeutralLineIcon Icon={CalendarTodayIcon} />}
				label={getTerm(TERMINOLOGY_KEYS.THROUGHPUT)}
				value={shownText(side.throughputPerDay, PER_DAY_DECIMALS, perDayText)}
			/>
			<PanelLine>
				<Typography component="span" sx={{ fontWeight: "bold" }}>
					{gap === null ? DASH : gapText(gap)}
				</Typography>
			</PanelLine>
			<AbsenceNotes notes={absenceNotesOf(sideProps)} />
		</>
	);
};

const SPACER_ROWS = Array.from({ length: ROWS_ABOVE_THE_GAP }, (_, row) => row);

/** The Work Items started against those finished per day in each window, compared on the gap between them. */
const ArrivalsPanel: React.FC<Readonly<ReportPanelProps>> = ({
	title,
	metric,
	workItems,
	directionOfGood,
}) => (
	<ThenNowPanelFrame
		title={title}
		info={
			<InfoPopoverButton
				label={`About ${title}`}
				text={`Arrivals are ${workItems} started. The gap is started minus finished, per day. Lower is better.`}
			/>
		}
		then={
			<ArrivalsLines
				side={metric.atThen}
				sideName="Then"
				workItems={workItems}
			/>
		}
		now={
			<ArrivalsLines side={metric.atNow} sideName="Now" workItems={workItems} />
		}
		change={
			<>
				{SPACER_ROWS.map((row) => (
					<PanelLine key={row}>{null}</PanelLine>
				))}
				<LineChanges
					metric={metric}
					lines={ARRIVALS_CHANGE_LINES}
					directionOfGood={directionOfGood}
					magnitudeText={perDayText}
				/>
			</>
		}
	/>
);

export default ArrivalsPanel;
