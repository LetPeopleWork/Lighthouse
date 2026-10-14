import DragHandleIcon from "@mui/icons-material/DragHandle";
import VerticalAlignBottomIcon from "@mui/icons-material/VerticalAlignBottom";
import VerticalAlignCenterIcon from "@mui/icons-material/VerticalAlignCenter";
import VerticalAlignTopIcon from "@mui/icons-material/VerticalAlignTop";
import type React from "react";
import type { ReportPanelProps } from "./reportPanelRegistry";
import {
	AbsenceNotes,
	LineChanges,
	NeutralLineIcon,
	type ThenNowSideProps,
	ValueLine,
} from "./ThenNowLineParts";
import ThenNowPanelFrame from "./ThenNowPanelFrame";
import { distinctAbsenceNotesOf, shownText } from "./thenNowComparison";

const DECIMALS = 0;

const SPREAD_LINES = [
	{
		key: "average",
		label: "Average",
		Icon: DragHandleIcon,
		decimals: DECIMALS,
	},
	{
		key: "median",
		label: "Median",
		Icon: VerticalAlignCenterIcon,
		decimals: DECIMALS,
	},
	{
		key: "min",
		label: "Min",
		Icon: VerticalAlignBottomIcon,
		decimals: DECIMALS,
	},
	{ key: "max", label: "Max", Icon: VerticalAlignTopIcon, decimals: DECIMALS },
] as const;

type ValueText = (value: number) => string;

const SpreadLines: React.FC<
	Readonly<ThenNowSideProps & { valueText: ValueText }>
> = ({ side, sideName, workItems, valueText }) => (
	<>
		{SPREAD_LINES.map(({ key, label, Icon, decimals }) => (
			<ValueLine
				key={key}
				label={label}
				value={shownText(side[key], decimals, valueText)}
				icon={<NeutralLineIcon Icon={Icon} />}
			/>
		))}
		<AbsenceNotes
			notes={distinctAbsenceNotesOf(SPREAD_LINES, side, sideName, workItems)}
		/>
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
		change={
			<LineChanges
				metric={metric}
				lines={SPREAD_LINES}
				directionOfGood={directionOfGood}
				magnitudeText={valueText}
			/>
		}
	/>
);

export default DailySpreadPanel;
