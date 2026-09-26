import { Stack, Typography } from "@mui/material";
import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	denominatorStatement,
	findings,
	levelLine,
	windowVerdict,
} from "./realityCheckCopy";

interface RealityCheckVerdictProps {
	result: RealityCheckResult;
}

const RealityCheckVerdict: React.FC<Readonly<RealityCheckVerdictProps>> = ({
	result,
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
			<Stack spacing={0.5}>
				{levelCoverage.map((level) => (
					<Typography key={level.confidenceLevel} variant="body2">
						{levelLine(level, denominator.runsEvaluated)}
					</Typography>
				))}
			</Stack>
			{findings(getTerm, levelCoverage.length).map((finding) => (
				<Typography key={finding} variant="body1">
					{finding}
				</Typography>
			))}
			<Typography variant="body2" color="text.secondary">
				{denominatorStatement(denominator)}
			</Typography>
		</Stack>
	);
};

export default RealityCheckVerdict;
