import type React from "react";
import { ForecastLevel } from "../../../../components/Common/Forecasts/ForecastLevel";
import type { ReportPanelProps } from "./reportPanelRegistry";
import {
	AbsenceNotes,
	LineChanges,
	type ThenNowSideProps,
	ValueLine,
} from "./ThenNowLineParts";
import ThenNowPanelFrame from "./ThenNowPanelFrame";
import {
	daysText,
	distinctAbsenceNotesOf,
	shownText,
} from "./thenNowComparison";

const DECIMALS = 0;

const PERCENTILES = [
	{ key: "p85", label: "85th", percentile: 85, decimals: DECIMALS },
	{ key: "p70", label: "70th", percentile: 70, decimals: DECIMALS },
	{ key: "p50", label: "50th", percentile: 50, decimals: DECIMALS },
] as const;

const PercentileLines: React.FC<Readonly<ThenNowSideProps>> = ({
	side,
	sideName,
	workItems,
}) => (
	<>
		{PERCENTILES.map(({ key, label, percentile, decimals }) => {
			const level = new ForecastLevel(percentile);
			const Icon = level.IconComponent;
			return (
				<ValueLine
					key={key}
					label={label}
					value={shownText(side[key], decimals, daysText)}
					icon={<Icon fontSize="small" sx={{ color: level.color }} />}
					color={level.color}
				/>
			);
		})}
		<AbsenceNotes
			notes={distinctAbsenceNotesOf(PERCENTILES, side, sideName, workItems)}
		/>
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
		change={
			<LineChanges
				metric={metric}
				lines={PERCENTILES}
				directionOfGood={directionOfGood}
				magnitudeText={daysText}
			/>
		}
	/>
);

export default CycleTimePanel;
