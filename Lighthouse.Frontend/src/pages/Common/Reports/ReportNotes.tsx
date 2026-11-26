import type React from "react";
import type { IReportOwner } from "../../../models/Reports/Report";

const ReportNotes: React.FC<
	Readonly<{ owner: IReportOwner; reportId: number; mayWrite: boolean }>
> = () => {
	throw new Error("Report notes are not shown yet.");
};

export default ReportNotes;
