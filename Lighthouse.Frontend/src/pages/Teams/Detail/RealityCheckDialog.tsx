import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";

// RED scaffold written by DISTILL for Story 6094; DELIVER replaces the body and removes the marker.
export const __SCAFFOLD__ = true;

export type RealityCheckRun =
	| { state: "running" }
	| { state: "answered"; result: RealityCheckResult }
	| { state: "failed"; message: string };

interface RealityCheckDialogProps {
	open: boolean;
	teamName: string;
	run: RealityCheckRun;
	onRunAgain: () => void;
	onClose: () => void;
}

const RealityCheckDialog: React.FC<Readonly<RealityCheckDialogProps>> = ({
	teamName,
}) => {
	throw new Error(
		`Not yet implemented -- RED scaffold: the reality check dialog for ${teamName}`,
	);
};

export default RealityCheckDialog;
