import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import { Box, Button, IconButton, Tooltip } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useRef, useState } from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import RealityCheckDialog, { type RealityCheckRun } from "./RealityCheckDialog";
import { triggerExplanation } from "./realityCheckCopy";

interface ForecastRealityCheckProps {
	teamId: number;
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
	applyFilterOverride,
}) => {
	const { forecastService } = useContext(ApiServiceContext);
	const reportUsage = useUsageDataReporter();
	const { getTerm } = useTerminology();
	const [isOpen, setIsOpen] = useState(false);
	const [run, setRun] = useState<RealityCheckRun>(RUNNING);
	// State updates land a render late, so a quick second press would still see the old values.
	const isRunningRef = useRef(false);
	const isOpenRef = useRef(false);

	// Leaving the tab mid-run closes the dialog as surely as closing it does.
	useEffect(
		() => () => {
			isOpenRef.current = false;
		},
		[],
	);

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

	// runCheck settles every failure itself, so the launched promise has nothing left to report.
	const startCheck = () => {
		runCheck().catch(() => undefined);
	};

	const openAndRun = () => {
		isOpenRef.current = true;
		setIsOpen(true);
		startCheck();
	};

	const close = () => {
		isOpenRef.current = false;
		setIsOpen(false);
	};

	return (
		<>
			<Box
				sx={{
					display: "flex",
					justifyContent: "flex-end",
					alignItems: "center",
					gap: 1,
				}}
			>
				<Tooltip title={triggerExplanation(getTerm)} describeChild>
					<IconButton aria-label="What does the reality check do?">
						<InfoOutlinedIcon fontSize="small" />
					</IconButton>
				</Tooltip>
				<Button variant="contained" onClick={openAndRun}>
					Run reality check
				</Button>
			</Box>
			<RealityCheckDialog
				open={isOpen}
				run={run}
				onRunAgain={startCheck}
				onClose={close}
			/>
		</>
	);
};

export default ForecastRealityCheck;
