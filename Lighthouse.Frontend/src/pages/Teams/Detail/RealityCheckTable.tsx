import {
	Stack,
	Table,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
} from "@mui/material";
import type React from "react";
import { useId } from "react";
import { ForecastLevel } from "../../../components/Common/Forecasts/ForecastLevel";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import RealityCheckPeriodGroup, {
	stickyHeaderCellSx,
} from "./RealityCheckPeriodGroup";
import { tableCaption } from "./realityCheckCopy";

interface RealityCheckTableProps {
	result: RealityCheckResult;
}

// The name and icon say which confidence level a column is, never how well it did, so the level's
// colour stays out of the header.
const LevelColumnHeader: React.FC<Readonly<{ confidenceLevel: number }>> = ({
	confidenceLevel,
}) => {
	const { level, IconComponent } = new ForecastLevel(confidenceLevel);
	return (
		<TableCell>
			<Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
				<span>{`${confidenceLevel}th `}</span>
				<IconComponent fontSize="small" aria-hidden="true" />
				<span>{level}</span>
			</Stack>
		</TableCell>
	);
};

// A plain table, never a data grid: a sortable grid would let a reader rank the sampling windows, and the
// checks cannot honestly be ranked against each other.
const RealityCheckTable: React.FC<Readonly<RealityCheckTableProps>> = ({
	result,
}) => {
	const { getTerm } = useTerminology();
	const captionId = useId();

	// A region that scrolls must take focus, or a keyboard alone could never scroll it.
	return (
		<TableContainer
			component="section"
			aria-labelledby={captionId}
			tabIndex={0}
			sx={{ overflowX: "auto" }}
		>
			<Table size="small" sx={{ captionSide: "top", minWidth: 640 }}>
				<caption id={captionId}>
					{tableCaption(result.teamName, getTerm)}
				</caption>
				<TableHead>
					<TableRow>
						<TableCell sx={stickyHeaderCellSx}>Sampling window</TableCell>
						{result.confidenceLevels.map((confidenceLevel) => (
							<LevelColumnHeader
								key={confidenceLevel}
								confidenceLevel={confidenceLevel}
							/>
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
