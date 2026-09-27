import { TableCell } from "@mui/material";
import type React from "react";
import { appColors, getContrastText } from "../../../utils/theme/colors";
import {
	gradedCellName,
	heldGlyph,
	heldWord,
	percentShown,
	signedMiss,
} from "./realityCheckCopy";
import { type GradedCheck, readCheck } from "./realityCheckGrading";

interface RealityCheckGradedCellProps {
	confidenceLevel: number;
	check: GradedCheck;
}

const RealityCheckGradedCell: React.FC<
	Readonly<RealityCheckGradedCellProps>
> = ({ confidenceLevel, check }) => {
	const { forecastValue, held } = check;
	const { grade, miss, percentOfActual } = readCheck(check);
	const fill = appColors.forecastGrade[grade];

	return (
		<TableCell
			aria-label={gradedCellName({
				confidenceLevel,
				forecastValue,
				miss,
				held,
				percentOfActual,
			})}
			style={{ backgroundColor: fill, color: getContrastText(fill) }}
		>
			<span>{forecastValue}</span> <span>{signedMiss(miss)}</span>{" "}
			{percentOfActual === null ? null : (
				<>
					<span>{percentShown(percentOfActual)}</span>{" "}
				</>
			)}
			<span>
				{heldGlyph(held)} {heldWord(held)}
			</span>
		</TableCell>
	);
};

export default RealityCheckGradedCell;
