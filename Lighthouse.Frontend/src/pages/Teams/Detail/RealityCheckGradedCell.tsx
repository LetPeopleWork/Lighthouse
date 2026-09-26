import { TableCell } from "@mui/material";
import type React from "react";
import {
	gradedCellName,
	heldGlyph,
	heldWord,
	signedMiss,
} from "./realityCheckCopy";
import { missOf } from "./realityCheckGrading";

interface RealityCheckGradedCellProps {
	confidenceLevel: number;
	forecastValue: number;
	actualCompleted: number;
	held: boolean;
}

const RealityCheckGradedCell: React.FC<
	Readonly<RealityCheckGradedCellProps>
> = ({ confidenceLevel, forecastValue, actualCompleted, held }) => {
	const miss = missOf({ forecastValue, actualCompleted });

	return (
		<TableCell
			aria-label={gradedCellName({
				confidenceLevel,
				forecastValue,
				miss,
				held,
			})}
		>
			<span>{forecastValue}</span> <span>{signedMiss(miss)}</span>{" "}
			<span>
				{heldGlyph(held)} {heldWord(held)}
			</span>
		</TableCell>
	);
};

export default RealityCheckGradedCell;
