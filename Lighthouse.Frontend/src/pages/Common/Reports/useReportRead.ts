import {
	type UseQueryOptions,
	useQuery,
	useQueryClient,
} from "@tanstack/react-query";
import type { IReport, IReportOwner } from "../../../models/Reports/Report";
import { reportQueryKeyOf } from "./reportOwner";
import { retriesUnlessFinal } from "./reportReadFailures";

/**
 * Reads a report under the one key the report view and the printed report share, so the fresh read a PDF is made from
 * also brings the view the reader is looking at up to date.
 */
export const useReportRead = (
	owner: IReportOwner,
	reportId: number,
	read: () => Promise<IReport>,
	options: Pick<UseQueryOptions<IReport>, "refetchOnMount"> = {},
) => {
	const usualRetry = useQueryClient().getDefaultOptions().queries?.retry;
	return useQuery({
		...options,
		queryKey: reportQueryKeyOf(owner, reportId),
		queryFn: read,
		retry: retriesUnlessFinal(usualRetry),
	});
};
