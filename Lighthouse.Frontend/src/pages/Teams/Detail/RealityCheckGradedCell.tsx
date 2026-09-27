import { TableCell, Tooltip } from "@mui/material";
import type React from "react";
import { useTerminology } from "../../../services/TerminologyContext";
import { appColors, getContrastText } from "../../../utils/theme/colors";
import { cellComparison, heldGlyph } from "./realityCheckCopy";
import { type GradedCheck, readCheck } from "./realityCheckGrading";

interface RealityCheckGradedCellProps {
	check: GradedCheck;
}

// The tooltip's title also becomes the cell's accessible name, so the words shown on demand and the
// words a screen reader hears are one and the same.
const RealityCheckGradedCell: React.FC<
	Readonly<RealityCheckGradedCellProps>
> = ({ check }) => {
	const { getTerm } = useTerminology();
	const { forecastValue, actualCompleted, held } = check;
	const { grade, miss, percentOfActual } = readCheck(check);
	const fill = appColors.forecastGrade[grade];

	return (
		<Tooltip
			title={cellComparison(
				{ forecastValue, actualCompleted, miss, percentOfActual },
				getTerm,
			)}
		>
			<TableCell
				style={{ backgroundColor: fill, color: getContrastText(fill) }}
			>
				{heldGlyph(held)} {forecastValue}
			</TableCell>
		</Tooltip>
	);
};

export default RealityCheckGradedCell;
