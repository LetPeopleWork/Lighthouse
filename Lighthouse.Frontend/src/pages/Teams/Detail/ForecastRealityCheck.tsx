import { Button } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useRef, useState } from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import RealityCheckDialog, { type RealityCheckRun } from "./RealityCheckDialog";

interface ForecastRealityCheckProps {
	teamId: number;
	teamName: string;
	applyFilterOverride?: boolean;
}

const RUNNING: RealityCheckRun = { state: "running" };

const FALLBACK_FAILURE_MESSAGE =
	"The reality check could not be run. Please try again.";

const failedRun = (error: unknown): RealityCheckRun => ({
	state: "failed",
	message: error instanceof Error ? error.message : FALLBACK_FAILURE_MESSAGE,
});

const ForecastRealityCheck: React.FC<ForecastRealityCheckProps> = ({
	teamId,
	teamName,
	applyFilterOverride,
}) => {
	const { forecastService } = useContext(ApiServiceContext);
	const reportUsage = useUsageDataReporter();
	const [isOpen, setIsOpen] = useState(false);
	const [run, setRun] = useState<RealityCheckRun>(RUNNING);
	// State updates land a render late, so a quick second press would still see the old values.
	const isRunningRef = useRef(false);
	const isOpenRef = useRef(false);

	const settleAnswer = useCallback(
		(result: RealityCheckResult) => {
			setRun({ state: "answered", result });
			reportUsage({ name: UsageDataEventName.TeamForecastRealityCheckRun });
		},
		[reportUsage],
	);

	const runCheck = useCallback(async () => {
		if (isRunningRef.current) {
			return;
		}

		isRunningRef.current = true;
		setRun(RUNNING);

		try {
			const result = await forecastService.runRealityCheck(
				teamId,
				applyFilterOverride,
			);
			// Nobody is looking at an answer that lands after the dialog closed; the next open asks afresh.
			if (isOpenRef.current) {
				settleAnswer(result);
			}
		} catch (error) {
			setRun(failedRun(error));
		} finally {
			isRunningRef.current = false;
		}
	}, [forecastService, teamId, applyFilterOverride, settleAnswer]);

	const openAndRun = () => {
		isOpenRef.current = true;
		setIsOpen(true);
		void runCheck();
	};

	const close = () => {
		isOpenRef.current = false;
		setIsOpen(false);
	};

	return (
		<>
			<Button
				variant="contained"
				onClick={openAndRun}
				sx={{ alignSelf: "flex-start" }}
			>
				Run reality check
			</Button>
			<RealityCheckDialog
				open={isOpen}
				teamName={teamName}
				run={run}
				onRunAgain={() => void runCheck()}
				onClose={close}
			/>
		</>
	);
};

export default ForecastRealityCheck;
