import { Stack } from "@mui/material";
import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import RealityCheckHeadline from "./RealityCheckHeadline";
import RealityCheckLevelRow from "./RealityCheckLevelRow";
import RealityCheckWindowBadge from "./RealityCheckWindowBadge";

interface RealityCheckSummaryProps {
	result: RealityCheckResult;
}

/** What a reader takes in at a glance above the table: the headline, the window badge and one row per level. */
const RealityCheckSummary: React.FC<Readonly<RealityCheckSummaryProps>> = ({
	result,
}) => (
	<Stack spacing={1.5}>
		<RealityCheckHeadline result={result} />
		<RealityCheckWindowBadge soundWindow={result.soundWindow} />
		<Stack spacing={1}>
			{[...result.levelCoverage]
				.sort((a, b) => a.confidenceLevel - b.confidenceLevel)
				.map((level) => (
					<RealityCheckLevelRow
						key={level.confidenceLevel}
						level={level}
						runsEvaluated={result.denominator.runsEvaluated}
						cells={result.cells}
					/>
				))}
		</Stack>
	</Stack>
);

export default RealityCheckSummary;
