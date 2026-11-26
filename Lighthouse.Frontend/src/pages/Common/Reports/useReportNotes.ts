import {
	type UseQueryResult,
	useQuery,
	useQueryClient,
} from "@tanstack/react-query";
import { useContext } from "react";
import type { IReportOwner } from "../../../models/Reports/Report";
import type { IReportNote } from "../../../models/Reports/ReportNote";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { reportQueryKeyOf } from "./reportOwner";
import { retriesUnlessFinal } from "./reportReadFailures";

export const reportNotesQueryKeyOf = (owner: IReportOwner, reportId: number) =>
	[...reportQueryKeyOf(owner, reportId), "notes"] as const;

export const useReportNotes = (
	owner: IReportOwner,
	reportId: number,
): UseQueryResult<IReportNote[]> => {
	const { reportService } = useContext(ApiServiceContext);
	const usualRetry = useQueryClient().getDefaultOptions().queries?.retry;
	return useQuery({
		queryKey: reportNotesQueryKeyOf(owner, reportId),
		queryFn: () => reportService.getNotes(owner, reportId),
		retry: retriesUnlessFinal(usualRetry),
	});
};
