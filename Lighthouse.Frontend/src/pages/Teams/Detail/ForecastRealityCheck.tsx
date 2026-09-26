import { Button, Stack } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useRef, useState } from "react";
import { useErrorSnackbar } from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import RealityCheckVerdict from "./RealityCheckVerdict";

interface ForecastRealityCheckProps {
	teamId: number;
	applyFilterOverride?: boolean;
}

const ForecastRealityCheck: React.FC<ForecastRealityCheckProps> = ({
	teamId,
	applyFilterOverride,
}) => {
	const { forecastService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();
	const reportUsage = useUsageDataReporter();
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
			reportUsage({ name: UsageDataEventName.TeamForecastRealityCheckRun });
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
	}, [forecastService, teamId, applyFilterOverride, showError, reportUsage]);

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
			{result && <RealityCheckVerdict result={result} />}
		</Stack>
	);
};

export default ForecastRealityCheck;
