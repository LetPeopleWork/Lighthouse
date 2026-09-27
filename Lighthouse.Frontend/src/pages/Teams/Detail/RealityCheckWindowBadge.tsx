import { Box, Chip, Tooltip } from "@mui/material";
import type React from "react";
import type { RealityCheckSoundWindow } from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	badgeWords,
	heldUpMeans,
	unevaluatedSentence,
} from "./realityCheckCopy";
import { type WindowBadgeState, windowBadgeOf } from "./realityCheckGrading";
import { useFocusTooltip } from "./useFocusTooltip";

const TONE: Record<WindowBadgeState, "success" | "warning" | "default"> = {
	Fine: "success",
	DidNotHoldUp: "warning",
	NoWindowHeldUp: "warning",
	CouldNotBeChecked: "default",
	FixedDates: "default",
	NotAPositiveLength: "default",
};

interface RealityCheckWindowBadgeProps {
	soundWindow: RealityCheckSoundWindow;
}

const RealityCheckWindowBadge: React.FC<
	Readonly<RealityCheckWindowBadgeProps>
> = ({ soundWindow }) => {
	const { getTerm } = useTerminology();
	const state = windowBadgeOf(soundWindow);
	const words = badgeWords[state](soundWindow.currentSettingDays, getTerm);
	const notChecked = unevaluatedSentence(soundWindow.unevaluatedWindowDays);
	const { tooltip, target } = useFocusTooltip();

	return (
		<Tooltip
			describeChild
			{...tooltip}
			title={
				<>
					<Box>{heldUpMeans}</Box>
					{notChecked === null ? null : <Box>{notChecked}</Box>}
				</>
			}
		>
			<Chip
				size="small"
				{...target}
				color={TONE[state]}
				label={words}
				sx={{ alignSelf: "flex-start" }}
			/>
		</Tooltip>
	);
};

export default RealityCheckWindowBadge;
