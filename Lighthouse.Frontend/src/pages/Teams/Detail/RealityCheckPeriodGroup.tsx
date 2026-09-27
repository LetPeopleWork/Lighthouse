import { TableBody, TableCell, TableRow } from "@mui/material";
import type { SxProps, Theme } from "@mui/material/styles";
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
import { type GradedCheck, gradedCheckAt } from "./realityCheckGrading";

// On a narrow screen the table scrolls sideways, and a reader must still see which period and which
// window a number belongs to. The cells need an opaque background to hide what scrolls beneath them; the
// dialog's paper carries its dark-theme elevation tint in --Paper-overlay, which the cells inherit.
export const stickyHeaderCellSx: SxProps<Theme> = {
	position: "sticky",
	left: 0,
	zIndex: 1,
	backgroundColor: "background.paper",
	backgroundImage: "var(--Paper-overlay)",
};

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
		<TableCell
			colSpan={levelCount}
			sx={{ color: "text.secondary", fontStyle: "italic" }}
		>
			{unevaluableRowCopy[reason]({
				daysWithCompletedWork: cell?.sufficiency.daysWithCompletedWork ?? 0,
				minimumActiveDays,
				getTerm,
			})}
		</TableCell>
	);
};

// A level the server left out of a check stays an empty cell, which the legend reads as not checked, so
// every other forecast still sits under its own column.
const LevelCell: React.FC<
	Readonly<{ confidenceLevel: number; check: GradedCheck | null }>
> = ({ confidenceLevel, check }) =>
	check === null ? (
		<TableCell />
	) : (
		<RealityCheckGradedCell confidenceLevel={confidenceLevel} check={check} />
	);

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
			{result.confidenceLevels.map((confidenceLevel) => (
				<LevelCell
					key={confidenceLevel}
					confidenceLevel={confidenceLevel}
					check={gradedCheckAt(
						{ levelOutcomes, actualCompleted },
						confidenceLevel,
					)}
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
					sx={stickyHeaderCellSx}
				>
					{horizonLabel(horizonDays)}
					{period && <PeriodFacts period={period} />}
				</TableCell>
			</TableRow>
			{result.sampledWindowDays.map((windowDays) => (
				<TableRow key={windowDays}>
					<TableCell component="th" scope="row" sx={stickyHeaderCellSx}>
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
