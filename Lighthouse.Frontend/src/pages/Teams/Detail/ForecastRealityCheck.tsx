import { Button } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useRef, useState } from "react";
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

const ForecastRealityCheck: React.FC<ForecastRealityCheckProps> = ({
	teamId,
	teamName,
	applyFilterOverride,
}) => {
	const { forecastService } = useContext(ApiServiceContext);
	const reportUsage = useUsageDataReporter();
	const [isOpen, setIsOpen] = useState(false);
	const [run, setRun] = useState<RealityCheckRun>(RUNNING);
	// State updates land a render late, so a quick second press would still see "not running".
	const isRunningRef = useRef(false);

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
			setRun({ state: "answered", result });
			reportUsage({ name: UsageDataEventName.TeamForecastRealityCheckRun });
		} catch (error) {
			setRun({
				state: "failed",
				message:
					error instanceof Error
						? error.message
						: "The reality check could not be run. Please try again.",
			});
		} finally {
			isRunningRef.current = false;
		}
	}, [forecastService, teamId, applyFilterOverride, reportUsage]);

	const openAndRun = () => {
		setIsOpen(true);
		void runCheck();
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
				onClose={() => setIsOpen(false)}
			/>
		</>
	);
};

export default ForecastRealityCheck;
