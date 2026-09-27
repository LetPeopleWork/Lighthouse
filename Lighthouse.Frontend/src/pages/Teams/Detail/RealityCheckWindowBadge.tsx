import { Chip } from "@mui/material";
import type React from "react";
import type { RealityCheckSoundWindow } from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import { badgeWords } from "./realityCheckCopy";
import { type WindowBadgeState, windowBadgeOf } from "./realityCheckGrading";

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

	return (
		<Chip
			size="small"
			tabIndex={0}
			color={TONE[state]}
			label={badgeWords[state](soundWindow.currentSettingDays, getTerm)}
			sx={{ alignSelf: "flex-start" }}
		/>
	);
};

export default RealityCheckWindowBadge;
