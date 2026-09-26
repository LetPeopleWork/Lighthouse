import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";

// RED scaffold written by DISTILL for Story 6094; DELIVER replaces the body and removes the marker.
export const __SCAFFOLD__ = true;

interface RealityCheckPeriodGroupProps {
	result: RealityCheckResult;
	horizonDays: number;
}

const RealityCheckPeriodGroup: React.FC<
	Readonly<RealityCheckPeriodGroupProps>
> = ({ horizonDays }) => {
	throw new Error(
		`Not yet implemented -- RED scaffold: the ${horizonDays}-day period group`,
	);
};

export default RealityCheckPeriodGroup;
