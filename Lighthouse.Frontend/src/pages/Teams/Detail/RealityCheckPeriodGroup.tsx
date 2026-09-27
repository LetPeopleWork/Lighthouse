import { TableBody, TableCell, TableRow } from "@mui/material";
import type { SxProps, Theme } from "@mui/material/styles";
import type React from "react";
import type {
	RealityCheckCell,
	RealityCheckResult,
} from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import RealityCheckGradedCell, {
	RealityCheckNotCheckedCell,
} from "./RealityCheckGradedCell";
import {
	periodHeader,
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

// Wide enough to hold the whole table, the header reads centred over it; on a narrow screen the table
// scrolls sideways, so the header keeps to the left edge where the reader can still see it.
const periodHeaderSx: SxProps<Theme> = {
	...stickyHeaderCellSx,
	position: { xs: "sticky", sm: "static" },
	textAlign: { xs: "left", sm: "center" },
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

// A level the server left out of a check keeps its cell, so every other forecast still sits under its
// own column.
const LevelCell: React.FC<Readonly<{ check: GradedCheck | null }>> = ({
	check,
}) =>
	check === null ? (
		<RealityCheckNotCheckedCell />
	) : (
		<RealityCheckGradedCell check={check} />
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
	const { getTerm } = useTerminology();

	return (
		<TableBody>
			<TableRow>
				<TableCell
					component="th"
					scope="rowgroup"
					colSpan={result.confidenceLevels.length + 1}
					sx={periodHeaderSx}
				>
					{periodHeader(horizonDays, period, getTerm)}
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
