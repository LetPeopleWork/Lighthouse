import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useContext } from "react";
import type { INewReportsForOwners } from "../../models/Reports/ReportsAcrossOwners";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../services/Api/UsageDataService";
import {
	usageDataReportTemplateFor,
	useUsageDataReporter,
} from "../../services/UsageData/usageDataReporter";
import { EVERY_REPORT_LIST_QUERY_KEY } from "../Common/Reports/reportOwner";

/**
 * One report created for several owners in one request; every report list is read again afterwards. The usage data
 * hears of every sent run, also one that creates nothing, and of each report it made; a lost answer tells of none.
 */
export const useBulkReportCreation = () => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	const reportUsage = useUsageDataReporter();
	return useMutation({
		mutationFn: (request: INewReportsForOwners) => {
			reportUsage({ name: UsageDataEventName.ReportBulkCreationStarted });
			return reportService.createReportsInBulk(request);
		},
		onSuccess: (outcomes, request) => {
			const reportTemplate = usageDataReportTemplateFor(request.templateKey);
			for (const outcome of outcomes) {
				if (outcome.status === "created") {
					reportUsage({
						name: UsageDataEventName.ReportCreated,
						reportTemplate,
					});
				}
			}
		},
		// Also after a failure: some reports may have been created before the answer was lost.
		onSettled: () =>
			queryClient.invalidateQueries({
				queryKey: EVERY_REPORT_LIST_QUERY_KEY,
			}),
	});
};
