import { Stack, Typography } from "@mui/material";
import type React from "react";
import type {
	RealityCheckCell,
	RealityCheckResult,
} from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	denominatorStatement,
	findings,
	levelLine,
	whyChecksCouldNotRun,
	windowVerdict,
} from "./realityCheckCopy";
import { type GradedCheck, levelCloseness } from "./realityCheckGrading";

// A cell lists its levels in its own order, so each level's check is found by its percentile, never by position.
const gradedChecksAt = (
	confidenceLevel: number,
	cells: readonly RealityCheckCell[],
): GradedCheck[] =>
	cells.flatMap(({ levelOutcomes, actualCompleted }) => {
		const outcome = levelOutcomes?.find(
			(candidate) => candidate.confidenceLevel === confidenceLevel,
		);
		if (outcome === undefined || actualCompleted === null) {
			return [];
		}
		return [
			{
				forecastValue: outcome.forecastValue,
				actualCompleted,
				held: outcome.held,
			},
		];
	});

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
