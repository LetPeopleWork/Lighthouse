import type React from "react";
import type { Team } from "../../../../models/Team/Team";

// RED scaffold written by DISTILL for Epic #6136; DELIVER replaces the body and removes the marker.
export const __SCAFFOLD__ = true;

interface RefinementViewProps {
	team: Team;
}

const RefinementView: React.FC<Readonly<RefinementViewProps>> = ({ team }) => {
	throw new Error(
		`Not yet implemented -- RED scaffold: the Refinement tab of ${team.name}`,
	);
};

export default RefinementView;
