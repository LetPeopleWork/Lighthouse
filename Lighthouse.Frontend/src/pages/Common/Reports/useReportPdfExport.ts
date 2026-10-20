import { useCallback } from "react";
import { useNavigate } from "react-router";
import type { IReportOwner } from "../../../models/Reports/Report";
import type { PrintOrigin } from "./print/usePrintOnce";
import { reportPathOf } from "./reportOwner";

/**
 * Starts a report's PDF export, from whichever surface offers it: the reader goes to the report's print page in the
 * same window. The wish to print travels in the history entry, never in the address, so a pasted link never prints.
 */
export const useReportPdfExport = () => {
	const navigate = useNavigate();

	return useCallback(
		(owner: IReportOwner, reportId: number, from: PrintOrigin) => {
			navigate(`/print${reportPathOf(owner, reportId)}`, {
				state: { printWhenReady: true, from },
			});
		},
		[navigate],
	);
};
