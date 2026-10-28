import { useQuery } from "@tanstack/react-query";
import { useContext } from "react";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";

/**
 * What a report needs, read again for every opening of the wizard: today and the owners' kept history move on.
 * Keyed by the opening, so a late answer to an earlier click never fills a newer wizard. Null while none is open.
 */
export const useReportCreationOptions = (opening: number | null) => {
	const { reportService } = useContext(ApiServiceContext);
	return useQuery({
		queryKey: ["reportCreationOptions", opening],
		queryFn: () => reportService.getCreationOptions(),
		enabled: opening !== null,
		staleTime: 0,
		refetchOnWindowFocus: false,
	});
};
