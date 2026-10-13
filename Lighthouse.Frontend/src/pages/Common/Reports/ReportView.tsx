import type React from "react";
import type { IReportOwner } from "../../../models/Reports/Report";

// Placeholder until this view is built; replaced with the real component.
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
