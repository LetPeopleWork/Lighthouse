import { type UseQueryResult, useQuery } from "@tanstack/react-query";
import type { IReportOwner } from "../../../models/Reports/Report";
import type { IReportNote } from "../../../models/Reports/ReportNote";
import { reportQueryKeyOf } from "./reportOwner";

export const reportNotesQueryKeyOf = (owner: IReportOwner, reportId: number) =>
	[...reportQueryKeyOf(owner, reportId), "notes"] as const;

export const useReportNotes = (
	owner: IReportOwner,
	reportId: number,
): UseQueryResult<IReportNote[]> =>
	useQuery({
		queryKey: reportNotesQueryKeyOf(owner, reportId),
		queryFn: () => {
			throw new Error("Report notes are not read yet.");
		},
	});
