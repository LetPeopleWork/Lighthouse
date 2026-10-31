import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useContext } from "react";
import type { IReportListItem } from "../../models/Reports/Report";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../services/UsageData/usageDataReporter";
import {
	EVERY_REPORT_LIST_QUERY_KEY,
	ownerOfReport,
	reportQueryKeyOf,
} from "../Common/Reports/reportOwner";

/**
 * Several reports removed in one request; every report list is read again afterwards, and each removed report, also
 * one already gone, is forgotten. The usage data hears of every sent removal and of each report this run removed;
 * one that was already gone was removed by somebody else.
 */
export const useBulkReportRemoval = () => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	const reportUsage = useUsageDataReporter();
	return useMutation({
		mutationFn: (reports: readonly IReportListItem[]) => {
			reportUsage({ name: UsageDataEventName.ReportBulkRemovalStarted });
			return reportService.removeReportsInBulk(
				reports.map((report) => report.id),
			);
		},
		onSuccess: (outcomes, reports) => {
			for (const outcome of outcomes) {
				if (outcome.status !== "removed") continue;
				if (outcome.code === undefined) {
					reportUsage({ name: UsageDataEventName.ReportDeleted });
				}
				const report = reports.find((picked) => picked.id === outcome.reportId);
				if (report !== undefined) {
					queryClient.removeQueries({
						queryKey: reportQueryKeyOf(ownerOfReport(report), report.id),
						exact: true,
					});
				}
			}
		},
		// Also after a failure: some reports may have been removed before the answer was lost.
		onSettled: () =>
			queryClient.invalidateQueries({
				queryKey: EVERY_REPORT_LIST_QUERY_KEY,
			}),
	});
};
