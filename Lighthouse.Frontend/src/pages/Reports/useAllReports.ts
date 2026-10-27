import { useQuery } from "@tanstack/react-query";
import { useContext } from "react";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";

/** Named for the whole "reports" family so that any change to a report refreshes this list too. */
export const ALL_REPORTS_QUERY_KEY = ["reports", "all"] as const;

export const useAllReports = () => {
	const { reportService } = useContext(ApiServiceContext);
	return useQuery({
		queryKey: ALL_REPORTS_QUERY_KEY,
		queryFn: () => reportService.getAllReports(),
		// Other users change them unseen, so every visit reads them again.
		staleTime: 0,
	});
};
