import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useContext } from "react";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";

/** Several reports removed in one request; every report list is read again afterwards. */
export const useBulkReportRemoval = () => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: (reportIds: number[]) =>
			reportService.removeReportsInBulk(reportIds),
		// Also after a failure: some reports may have been removed before the answer was lost.
		onSettled: () => queryClient.invalidateQueries({ queryKey: ["reports"] }),
	});
};
