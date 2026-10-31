import type { PrintedReport } from "./ReportPrintBundle";

const ONE_REPORT = /^(team|portfolio)-(\d+)-(\d+)$/;

/** The reports named by `team-3-12,portfolio-5-7` (owner kind, owner id, report id), in that order; anything else is left out. */
export const printedReportsOf = (query: string | null): PrintedReport[] =>
	(query ?? "").split(",").flatMap((part) => {
		const match = ONE_REPORT.exec(part.trim());
		if (match === null) return [];
		const [, kind, ownerId, reportId] = match;
		return [
			{
				owner: {
					kind: kind as PrintedReport["owner"]["kind"],
					id: Number(ownerId),
				},
				reportId: Number(reportId),
			},
		];
	});
