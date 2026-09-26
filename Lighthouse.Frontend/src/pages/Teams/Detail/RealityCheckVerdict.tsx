import { Stack, Typography } from "@mui/material";
import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import { NominalRateLines } from "./RealityCheckEvidence";
import {
	denominatorStatement,
	findings,
	whyChecksCouldNotRun,
	windowVerdict,
} from "./realityCheckCopy";

interface RealityCheckVerdictProps {
	result: RealityCheckResult;
	// Open evidence ends with these same lines under its panels; showing them here as well would print
	// every level twice.
	showsLevels: boolean;
}

const RealityCheckVerdict: React.FC<Readonly<RealityCheckVerdictProps>> = ({
	result,
	showsLevels,
}) => {
	const { getTerm } = useTerminology();
	const { denominator, levelCoverage } = result;

	return (
		<Stack spacing={1.5}>
			<Typography variant="body1">
				{windowVerdict(
					result.teamName,
					result.sampledWindowDays,
					result.soundWindow,
					getTerm,
				)}
			</Typography>
			{showsLevels && <NominalRateLines result={result} />}
			{findings(getTerm, levelCoverage.length).map((finding) => (
				<Typography key={finding} variant="body1">
					{finding}
				</Typography>
			))}
			<Typography variant="body2" color="text.secondary">
				{denominatorStatement(
					denominator,
					whyChecksCouldNotRun(
						result.cells,
						result.sampledWindowDays,
						result.minimumActiveDays,
						getTerm,
					),
				)}
			</Typography>
		</Stack>
	);
};

export default RealityCheckVerdict;
