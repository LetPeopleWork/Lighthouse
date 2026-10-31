import type { GridRowId } from "@mui/x-data-grid";
import { useRef, useState } from "react";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";

/** The reports the reader picked, in the order picked, and a way to read them in the order the table shows them. */
export const useRowPicks = (
	reports: readonly IReportAcrossOwners[] | undefined,
) => {
	const [selectedIds, setSelectedIds] = useState<GridRowId[]>([]);
	const tableOrder = useRef<(() => GridRowId[]) | undefined>(undefined);
	const reportOf = (id: GridRowId) =>
		reports?.find((report) => report.id === id) ?? [];
	const picked = selectedIds.flatMap((id) => reportOf(id));
	const inTableOrder = (): IReportAcrossOwners[] =>
		(tableOrder.current?.() ?? selectedIds)
			.filter((id) => selectedIds.includes(id))
			.flatMap((id) => reportOf(id));
	return {
		picking: { selectedIds, onSelectionChange: setSelectedIds, tableOrder },
		picked,
		inTableOrder,
		clear: () => setSelectedIds([]),
		unpick: (unpicked: readonly IReportAcrossOwners[]) =>
			setSelectedIds((ids) =>
				ids.filter((id) => !unpicked.some((report) => report.id === id)),
			),
	};
};

export type RowPicks = ReturnType<typeof useRowPicks>;
