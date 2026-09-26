import { Button, Stack, Typography } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useRef, useState } from "react";
import { useErrorSnackbar } from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type {
	RealityCheckDenominator,
	RealityCheckResult,
} from "../../../models/Forecasts/RealityCheckResult";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import RealityCheckVerdict from "./RealityCheckVerdict";

interface ForecastRealityCheckProps {
	teamId: number;
	applyFilterOverride?: boolean;
}

const denominatorLine = ({
	runsEvaluated,
	levelsPerRun,
	scoresEvaluated,
}: RealityCheckDenominator): string =>
	[
		`${runsEvaluated} forecast runs were checked, each read at ${levelsPerRun} confidence levels — ${scoresEvaluated} scores in all.`,
		`The ${levelsPerRun} levels of a single run come from the same simulation, so they are not independent of one another.`,
		"And each run covers a different stretch of real time — every one ends today and reaches back by its own length — so they are not repeated trials of one experiment and should not be ranked against each other.",
	].join(" ");

const ForecastRealityCheck: React.FC<ForecastRealityCheckProps> = ({
	teamId,
	applyFilterOverride,
}) => {
	const { forecastService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();
	const [result, setResult] = useState<RealityCheckResult | null>(null);
	const [isRunning, setIsRunning] = useState(false);
	// State updates land a render late, so a quick second press would still see "not running".
	const isRunningRef = useRef(false);

	const runCheck = useCallback(async () => {
		if (isRunningRef.current) {
			return;
		}

		isRunningRef.current = true;
		setIsRunning(true);
		setResult(null);

		try {
			setResult(
				await forecastService.runRealityCheck(teamId, applyFilterOverride),
			);
		} catch (error) {
			showError(
				error instanceof Error
					? error.message
					: "The reality check could not be run. Please try again.",
			);
		} finally {
			isRunningRef.current = false;
			setIsRunning(false);
		}
	}, [forecastService, teamId, applyFilterOverride, showError]);

	return (
		<Stack spacing={2} sx={{ width: "100%" }}>
			<Button
				variant="contained"
				onClick={runCheck}
				loading={isRunning}
				sx={{ alignSelf: "flex-start" }}
			>
				Run reality check
			</Button>
			{result && (
				<>
					<RealityCheckVerdict result={result} />
					<Typography variant="body2" color="text.secondary">
						{denominatorLine(result.denominator)}
					</Typography>
				</>
			)}
		</Stack>
	);
};

export default ForecastRealityCheck;
