import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useContext } from "react";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../services/UsageData/usageDataReporter";
import { EVERY_REPORT_LIST_QUERY_KEY } from "../Common/Reports/reportOwner";

/**
 * Several reports removed in one request; every report list is read again afterwards. The usage data hears of every
 * sent removal and of each report this run removed; one that was already gone was removed by somebody else.
 */
export const useBulkReportRemoval = () => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	const reportUsage = useUsageDataReporter();
	return useMutation({
		mutationFn: (reportIds: number[]) => {
			reportUsage({ name: UsageDataEventName.ReportBulkRemovalStarted });
			return reportService.removeReportsInBulk(reportIds);
		},
		onSuccess: (outcomes) => {
			for (const outcome of outcomes) {
				if (outcome.status === "removed" && outcome.code === undefined) {
					reportUsage({ name: UsageDataEventName.ReportDeleted });
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
