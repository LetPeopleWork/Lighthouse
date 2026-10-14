import { useQuery } from "@tanstack/react-query";
import { useContext } from "react";
import type { IReportOwner } from "../../../models/Reports/Report";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { reportsQueryKeyOf } from "./reportOwner";

export const useOwnerReports = (owner: IReportOwner) => {
	const { reportService } = useContext(ApiServiceContext);
	return useQuery({
		queryKey: reportsQueryKeyOf(owner),
		queryFn: () => reportService.getReports(owner),
	});
};
