import { TableBody, TableCell, TableRow } from "@mui/material";
import type React from "react";
import type {
	RealityCheckCell,
	RealityCheckResult,
	RealityCheckScoredPeriod,
} from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import RealityCheckGradedCell from "./RealityCheckGradedCell";
import {
	dayInWords,
	horizonLabel,
	periodActual,
	unevaluableRowCopy,
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

const UnevaluableCells: React.FC<
	Readonly<{
		cell: RealityCheckCell | undefined;
		levelCount: number;
		minimumActiveDays: number;
	}>
> = ({ cell, levelCount, minimumActiveDays }) => {
	const { getTerm } = useTerminology();
	const reason = cell?.sufficiency.reason ?? "Sufficient";
	return (
		<TableCell colSpan={levelCount}>
			{unevaluableRowCopy[reason]({
				daysWithCompletedWork: cell?.sufficiency.daysWithCompletedWork ?? 0,
				minimumActiveDays,
				getTerm,
			})}
		</TableCell>
	);
};

const CheckCells: React.FC<
	Readonly<{ cell: RealityCheckCell | undefined; result: RealityCheckResult }>
> = ({ cell, result }) => {
	const levelOutcomes = cell?.levelOutcomes ?? null;
	const actualCompleted = cell?.actualCompleted ?? null;
	if (levelOutcomes === null || actualCompleted === null) {
		return (
			<UnevaluableCells
				cell={cell}
				levelCount={result.confidenceLevels.length}
				minimumActiveDays={result.minimumActiveDays}
			/>
		);
	}
	return (
		<>
			{levelOutcomes.map((level) => (
				<RealityCheckGradedCell
					key={level.confidenceLevel}
					confidenceLevel={level.confidenceLevel}
					forecastValue={level.forecastValue}
					actualCompleted={actualCompleted}
					held={level.held}
				/>
			))}
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
					<CheckCells
						cell={result.cells.find(
							(cell) =>
								cell.horizonDays === horizonDays &&
								cell.samplingWindowDays === windowDays,
						)}
						result={result}
					/>
				</TableRow>
			))}
		</TableBody>
	);
};

export default RealityCheckPeriodGroup;
