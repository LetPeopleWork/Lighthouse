import { useCallback } from "react";
import { useNavigate } from "react-router";
import type { IReport, IReportOwner } from "../../../models/Reports/Report";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import {
	usageDataReportTemplateOf,
	useUsageDataReporter,
} from "../../../services/UsageData/usageDataReporter";
import { printRequestFor } from "./print/printRequest";
import type { PrintOrigin } from "./print/usePrintOnce";
import { reportPathOf } from "./reportOwner";

/**
 * Starts a report's PDF export, from whichever surface offers it: the export is counted with the report's Type,
 * then the reader goes to the report's print page in the same window. The wish to print travels in the history
 * entry, never in the address, so a pasted link never prints.
 */
export const useReportPdfExport = () => {
	const navigate = useNavigate();
	const reportUsage = useUsageDataReporter();

	return useCallback(
		(
			owner: IReportOwner,
			report: Pick<IReport, "id" | "templateKey">,
			from: PrintOrigin,
		) => {
			const template = usageDataReportTemplateOf(report.templateKey);
			if (template !== undefined) {
				reportUsage({
					name: UsageDataEventName.ReportPdfExportStarted,
					reportTemplate: template,
				});
			}

			navigate(`/print${reportPathOf(owner, report.id)}`, {
				state: printRequestFor(from),
			});
		},
		[navigate, reportUsage],
	);
};
