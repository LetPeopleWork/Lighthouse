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
import { readCheck } from "./realityCheckGrading";

interface RealityCheckGradedCellProps {
	confidenceLevel: number;
	forecastValue: number;
	actualCompleted: number;
	held: boolean;
}

const RealityCheckGradedCell: React.FC<
	Readonly<RealityCheckGradedCellProps>
> = ({ confidenceLevel, forecastValue, actualCompleted, held }) => {
	const { grade, miss, percentOfActual } = readCheck({
		forecastValue,
		actualCompleted,
		held,
	});
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
