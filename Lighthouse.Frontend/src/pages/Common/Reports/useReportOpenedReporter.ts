import { type QueryClient, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useLocation } from "react-router";
import type { IReport } from "../../../models/Reports/Report";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { DWELL_BEFORE_A_PAGE_COUNTS_MS } from "../../../services/UsageData/usageDataEvents";
import {
	usageDataReportTemplateOf,
	useUsageDataReporter,
} from "../../../services/UsageData/usageDataReporter";

/**
 * The history entries whose opening was already counted. Kept with the app's query cache, which lives as long as the
 * app does, rather than in this module: a newly made router's first entry always carries the same key, so a record
 * shared by every router would take one router's count for another's.
 */
const countedOpenings = new WeakMap<QueryClient, Set<string>>();

const openingsCountedIn = (app: QueryClient): Set<string> => {
	const known = countedOpenings.get(app);
	if (known !== undefined) return known;
	const counted = new Set<string>();
	countedOpenings.set(app, counted);
	return counted;
};

/**
 * Reports that a report was opened, with its Type, once it has been on screen as long as a tab must be to count as
 * opened. One opening is one entry in the reader's history: reading the report again after an edit, or coming back
 * to the same entry after printing, is the same opening, and counts only if it had not counted before. A report that
 * never loaded was not read, so it reports nothing.
 */
export const useReportOpenedReporter = (report: IReport | undefined): void => {
	const reportUsage = useUsageDataReporter();
	const opening = useLocation().key;
	const counted = openingsCountedIn(useQueryClient());
	const template =
		report === undefined
			? undefined
			: usageDataReportTemplateOf(report.templateKey);

	useEffect(() => {
		if (counted.has(opening) || template === undefined) {
			return;
		}

		const dwelt = setTimeout(() => {
			// Consent can arrive after the report is shown; until the event could go, the opening is not used up.
			const sent = reportUsage({
				name: UsageDataEventName.ReportOpened,
				reportTemplate: template,
			});
			if (sent) counted.add(opening);
		}, DWELL_BEFORE_A_PAGE_COUNTS_MS);

		return () => clearTimeout(dwelt);
	}, [counted, opening, template, reportUsage]);
};
