import {
	Table,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
} from "@mui/material";
import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import RealityCheckLevelLabel from "./RealityCheckLevelLabel";
import RealityCheckPeriodGroup, {
	stickyHeaderCellSx,
} from "./RealityCheckPeriodGroup";
import { tableRegionName } from "./realityCheckCopy";

interface RealityCheckTableProps {
	result: RealityCheckResult;
}

// A plain table, never a data grid: a sortable grid would let a reader rank the sampling windows, and the
// checks cannot honestly be ranked against each other.
const RealityCheckTable: React.FC<Readonly<RealityCheckTableProps>> = ({
	result,
}) => {
	// A region that scrolls must take focus, or a keyboard alone could never scroll it.
	return (
		<TableContainer
			component="section"
			aria-label={tableRegionName(result.teamName)}
			tabIndex={0}
			sx={{ overflowX: "auto" }}
		>
			<Table size="small" sx={{ minWidth: 640 }}>
				<TableHead>
					<TableRow>
						<TableCell sx={stickyHeaderCellSx}>Sampling window</TableCell>
						{result.confidenceLevels.map((confidenceLevel) => (
							<TableCell key={confidenceLevel}>
								<RealityCheckLevelLabel confidenceLevel={confidenceLevel} />
							</TableCell>
						))}
					</TableRow>
				</TableHead>
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
};

export default RealityCheckTable;
