import type React from "react";
import type { IReportOwner } from "../../../models/Reports/Report";

// RED scaffold written by DISTILL for Epic #5878; DELIVER replaces the body and removes the marker.
export const __SCAFFOLD__ = true;

interface ReportViewProps {
	owner: IReportOwner;
	reportId: number;
}

const ReportView: React.FC<Readonly<ReportViewProps>> = ({
	owner,
	reportId,
}) => {
	throw new Error(
		`Not yet implemented -- RED scaffold: report ${reportId} of ${owner.kind} ${owner.id}`,
	);
};

export default ReportView;
