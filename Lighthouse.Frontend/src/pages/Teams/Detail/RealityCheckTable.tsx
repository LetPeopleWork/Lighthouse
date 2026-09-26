import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";

// RED scaffold written by DISTILL for Story 6094; DELIVER replaces the body and removes the marker.
export const __SCAFFOLD__ = true;

interface RealityCheckTableProps {
	result: RealityCheckResult;
}

const RealityCheckTable: React.FC<Readonly<RealityCheckTableProps>> = ({
	result,
}) => {
	throw new Error(
		`Not yet implemented -- RED scaffold: the table of checks for ${result.teamName}`,
	);
};

export default RealityCheckTable;
