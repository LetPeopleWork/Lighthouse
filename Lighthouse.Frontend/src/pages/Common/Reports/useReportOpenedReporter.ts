import { useEffect, useRef } from "react";
import { useLocation } from "react-router";
import type { IReport } from "../../../models/Reports/Report";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { DWELL_BEFORE_A_PAGE_COUNTS_MS } from "../../../services/UsageData/usageDataEvents";
import {
	usageDataReportTemplateOf,
	useUsageDataReporter,
} from "../../../services/UsageData/usageDataReporter";
import { RETURNED_FROM_PRINT } from "./print/usePrintOnce";

const isReturnFromPrint = (state: unknown): boolean =>
	typeof state === "object" &&
	state !== null &&
	"returnedFromPrint" in state &&
	state.returnedFromPrint === RETURNED_FROM_PRINT.returnedFromPrint;

/**
 * Reports that a report was opened, with its Type, once it has been on screen as long as a tab must be
 * to count as opened. Reported at most once while the view stays open: reading the report again after
 * an edit is the same opening. A report that never loaded was not read, so it reports nothing. Coming back from
 * printing is the same reading as well, so the view a print returns to reports nothing.
 */
export const useReportOpenedReporter = (report: IReport | undefined): void => {
	const reportUsage = useUsageDataReporter();
	const hasReported = useRef(isReturnFromPrint(useLocation().state));
	const template =
		report === undefined
			? undefined
			: usageDataReportTemplateOf(report.templateKey);

	useEffect(() => {
		if (hasReported.current || template === undefined) {
			return;
		}

		const counted = setTimeout(() => {
			// Consent can arrive after the report is shown; until the event could go, the opening is not used up.
			hasReported.current = reportUsage({
				name: UsageDataEventName.ReportOpened,
				reportTemplate: template,
			});
		}, DWELL_BEFORE_A_PAGE_COUNTS_MS);

		return () => clearTimeout(counted);
	}, [template, reportUsage]);
};
