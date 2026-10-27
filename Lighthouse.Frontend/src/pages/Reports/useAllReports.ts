import { useQuery } from "@tanstack/react-query";
import { useContext } from "react";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { ALL_REPORTS_QUERY_KEY } from "../Common/Reports/reportOwner";

export const useAllReports = () => {
	const { reportService } = useContext(ApiServiceContext);
	return useQuery({
		queryKey: ALL_REPORTS_QUERY_KEY,
		queryFn: () => reportService.getAllReports(),
		// Other users change them unseen, so every visit reads them again.
		staleTime: 0,
	});
};
