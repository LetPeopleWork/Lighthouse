import type React from "react";
import type { IReportOwner } from "../../../models/Reports/Report";

// RED scaffold written by DISTILL for Epic #5878; DELIVER replaces the body and removes the marker.
export const __SCAFFOLD__ = true;

interface ReportsTabProps {
	owner: IReportOwner;
}

const ReportsTab: React.FC<Readonly<ReportsTabProps>> = ({ owner }) => {
	throw new Error(
		`Not yet implemented -- RED scaffold: the Reports tab of ${owner.kind} ${owner.id}`,
	);
};

export default ReportsTab;
