import type React from "react";

// RED scaffold written by DISTILL for Story 6094; DELIVER replaces the body and removes the marker.
export const __SCAFFOLD__ = true;

interface RealityCheckGradedCellProps {
	confidenceLevel: number;
	forecastValue: number;
	actualCompleted: number;
	held: boolean;
}

const RealityCheckGradedCell: React.FC<
	Readonly<RealityCheckGradedCellProps>
> = ({ confidenceLevel }) => {
	throw new Error(
		`Not yet implemented -- RED scaffold: the ${confidenceLevel}th level of one check`,
	);
};

export default RealityCheckGradedCell;
