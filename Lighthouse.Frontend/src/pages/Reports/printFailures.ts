import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import type { PrintedReport } from "../Common/Reports/print/bundleState";

export const isTheSameReport = (
	report: IReportAcrossOwners,
	{ owner, reportId }: PrintedReport,
): boolean =>
	report.id === reportId &&
	report.ownerKind === owner.kind &&
	report.ownerId === owner.id;

export const failedAmong = (
	reports: readonly IReportAcrossOwners[],
	failedReports: readonly PrintedReport[],
): IReportAcrossOwners[] =>
	reports.filter((report) =>
		failedReports.some((failed) => isTheSameReport(report, failed)),
	);

/**
 * What is still printed when the failed reports are left out: offered only when something failed and something
 * would be left, and then only the failed reports go. Undefined when leaving them out is not offered.
 */
export const leaveOutTheFailed = (
	reports: readonly IReportAcrossOwners[],
	failed: readonly IReportAcrossOwners[],
): IReportAcrossOwners[] | undefined => {
	const rest = reports.filter((report) => !failed.includes(report));
	if (failed.length === 0 || rest.length === 0) return undefined;
	return rest;
};
