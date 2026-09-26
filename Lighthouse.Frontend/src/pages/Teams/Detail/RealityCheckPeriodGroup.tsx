import { TableBody, TableCell, TableRow } from "@mui/material";
import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { horizonLabel } from "./realityCheckCopy";

interface RealityCheckPeriodGroupProps {
	result: RealityCheckResult;
	horizonDays: number;
}

const RealityCheckPeriodGroup: React.FC<
	Readonly<RealityCheckPeriodGroupProps>
> = ({ result, horizonDays }) => (
	<TableBody>
		<TableRow>
			<TableCell component="th" scope="rowgroup">
				{horizonLabel(horizonDays)}
			</TableCell>
		</TableRow>
		{result.sampledWindowDays.map((windowDays) => (
			<TableRow key={windowDays}>
				<TableCell component="th" scope="row">
					{windowDays} days
				</TableCell>
			</TableRow>
		))}
	</TableBody>
);

export default RealityCheckPeriodGroup;
