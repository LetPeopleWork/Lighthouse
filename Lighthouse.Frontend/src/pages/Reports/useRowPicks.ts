import type { GridRowId } from "@mui/x-data-grid";
import { useRef, useState } from "react";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import { pickedReports } from "./pickedReports";

/** The reports the reader picked, in the order picked, and a way to read them in the order the table shows them. */
export const useRowPicks = (
	reports: readonly IReportAcrossOwners[] | undefined,
) => {
	const [selectedIds, setSelectedIds] = useState<GridRowId[]>([]);
	const tableOrder = useRef<(() => GridRowId[]) | undefined>(undefined);
	const picked = pickedReports(reports, selectedIds);
	const inTableOrder = (): IReportAcrossOwners[] =>
		(tableOrder.current?.() ?? selectedIds)
			.filter((id) => selectedIds.includes(id))
			.flatMap((id) => pickedReports(reports, [id]));
	return {
		picking: { selectedIds, onSelectionChange: setSelectedIds, tableOrder },
		picked,
		inTableOrder,
		clear: () => setSelectedIds([]),
		unpick: (unpicked: readonly IReportAcrossOwners[]) => {
			const unpickedIds = new Set<GridRowId>(
				unpicked.map((report) => report.id),
			);
			setSelectedIds((ids) => ids.filter((id) => !unpickedIds.has(id)));
		},
	};
};

export type RowPicks = ReturnType<typeof useRowPicks>;
