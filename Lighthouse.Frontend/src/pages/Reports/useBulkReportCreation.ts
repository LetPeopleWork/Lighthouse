import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useContext } from "react";
import type { INewReportsForOwners } from "../../models/Reports/ReportsAcrossOwners";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";

/** One report created for several owners in one request; every report list is read again afterwards. */
export const useBulkReportCreation = () => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: (request: INewReportsForOwners) =>
			reportService.createReportsInBulk(request),
		// Also after a failure: some reports may have been created before the answer was lost.
		onSettled: () => queryClient.invalidateQueries({ queryKey: ["reports"] }),
	});
};
