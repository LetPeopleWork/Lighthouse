import type { PrintedReport } from "./bundleState";

const ONE_REPORT = /^(team|portfolio)-(\d+)-(\d+)$/;

const printedReportIn = (part: string): PrintedReport | undefined => {
	const match = ONE_REPORT.exec(part.trim());
	if (match === null) return undefined;
	const [, kind, ownerId, reportId] = match;
	return {
		owner: {
			kind: kind as PrintedReport["owner"]["kind"],
			id: Number(ownerId),
		},
		reportId: Number(reportId),
	};
};

/**
 * The reports named by `team-3-12,portfolio-5-7` (owner kind, owner id, report id), in that order; anything else is
 * left out, and a report named again is drawn only where it was first named.
 */
export const printedReportsOf = (query: string | null): PrintedReport[] => {
	const named = new Map<string, PrintedReport>();
	for (const part of (query ?? "").split(",")) {
		const report = printedReportIn(part);
		if (report === undefined) continue;
		const key = `${report.owner.kind}-${report.owner.id}-${report.reportId}`;
		if (!named.has(key)) named.set(key, report);
	}
	return [...named.values()];
};
