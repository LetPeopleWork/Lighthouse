import type { GridRowId } from "@mui/x-data-grid";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";

/** The listed reports among the ids, in the order the ids are given; an id that is not listed picks nothing. */
export const pickedReports = (
	reports: readonly IReportAcrossOwners[] | undefined,
	ids: readonly GridRowId[],
): IReportAcrossOwners[] =>
	ids.flatMap((id) => reports?.find((report) => report.id === id) ?? []);

/** What the page says about how many reports are picked. */
export const selectedSentence = (picked: number): string =>
	`${picked} selected`;
