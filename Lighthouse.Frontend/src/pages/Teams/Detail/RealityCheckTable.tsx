import { Table, TableContainer } from "@mui/material";
import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import RealityCheckPeriodGroup from "./RealityCheckPeriodGroup";

interface RealityCheckTableProps {
	result: RealityCheckResult;
}

// A plain table, never a data grid: a sortable grid would let a reader rank the sampling windows, and the
// checks cannot honestly be ranked against each other.
const RealityCheckTable: React.FC<Readonly<RealityCheckTableProps>> = ({
	result,
}) => (
	<TableContainer>
		<Table size="small">
			{result.sampledHorizonDays.map((horizonDays) => (
				<RealityCheckPeriodGroup
					key={horizonDays}
					result={result}
					horizonDays={horizonDays}
				/>
			))}
		</Table>
	</TableContainer>
);

export default RealityCheckTable;
