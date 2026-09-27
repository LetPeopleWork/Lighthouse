import { Stack, Typography } from "@mui/material";
import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import RealityCheckHeadline from "./RealityCheckHeadline";
import RealityCheckLevelRow from "./RealityCheckLevelRow";
import RealityCheckWindowBadge from "./RealityCheckWindowBadge";
import {
	denominatorStatement,
	findings,
	levelLine,
	whyChecksCouldNotRun,
	windowVerdict,
} from "./realityCheckCopy";
import { gradedChecksAt, levelCloseness } from "./realityCheckGrading";

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
			<RealityCheckHeadline denominator={denominator} />
			<RealityCheckWindowBadge soundWindow={result.soundWindow} />
			<Stack spacing={1}>
				{[...levelCoverage]
					.sort((a, b) => a.confidenceLevel - b.confidenceLevel)
					.map((level) => (
						<RealityCheckLevelRow
							key={level.confidenceLevel}
							level={level}
							runsEvaluated={denominator.runsEvaluated}
							cells={result.cells}
						/>
					))}
			</Stack>
			<Typography variant="subtitle1" component="h3">
				How often each confidence level held
			</Typography>
			<Stack spacing={0.5}>
				{levelCoverage.map((level) => (
					<Typography key={level.confidenceLevel} variant="body1">
						{levelLine(
							level,
							denominator.runsEvaluated,
							levelCloseness(
								gradedChecksAt(level.confidenceLevel, result.cells),
							),
						)}
					</Typography>
				))}
			</Stack>
			<Typography variant="body1">
				{windowVerdict(
					result.teamName,
					result.sampledWindowDays,
					result.soundWindow,
					getTerm,
				)}
			</Typography>
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
