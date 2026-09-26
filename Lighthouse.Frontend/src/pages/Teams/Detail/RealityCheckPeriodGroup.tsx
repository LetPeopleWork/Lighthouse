import { TableBody, TableCell, TableRow } from "@mui/material";
import type React from "react";
import type {
	RealityCheckResult,
	RealityCheckScoredPeriod,
} from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	dayInWords,
	horizonLabel,
	periodActual,
	windowRowLabel,
} from "./realityCheckCopy";

interface RealityCheckPeriodGroupProps {
	result: RealityCheckResult;
	horizonDays: number;
}

const PeriodDay: React.FC<Readonly<{ isoDay: string }>> = ({ isoDay }) => (
	<time dateTime={isoDay}>{dayInWords(isoDay)}</time>
);

const PeriodFacts: React.FC<Readonly<{ period: RealityCheckScoredPeriod }>> = ({
	period,
}) => {
	const { getTerm } = useTerminology();
	return (
		<>
			{", "}
			<PeriodDay isoDay={period.scoredPeriodStart} />
			{" to "}
			<PeriodDay isoDay={period.scoredPeriodEnd} />
			{": "}
			<span>{periodActual(period.actualCompleted, getTerm)}</span>
		</>
	);
};

const RealityCheckPeriodGroup: React.FC<
	Readonly<RealityCheckPeriodGroupProps>
> = ({ result, horizonDays }) => {
	const period = result.scoredPeriods.find(
		(scored) => scored.horizonDays === horizonDays,
	);
	const { currentSettingDays, currentSettingWasTested } = result.soundWindow;

	return (
		<TableBody>
			<TableRow>
				<TableCell
					component="th"
					scope="rowgroup"
					colSpan={result.confidenceLevels.length + 1}
				>
					{horizonLabel(horizonDays)}
					{period && <PeriodFacts period={period} />}
				</TableCell>
			</TableRow>
			{result.sampledWindowDays.map((windowDays) => (
				<TableRow key={windowDays}>
					<TableCell component="th" scope="row">
						{windowRowLabel(
							windowDays,
							currentSettingWasTested && windowDays === currentSettingDays,
						)}
					</TableCell>
				</TableRow>
			))}
		</TableBody>
	);
};

export default RealityCheckPeriodGroup;
