import { Box } from "@mui/material";
import type { GridColDef, GridValidRowModel } from "@mui/x-data-grid";
import { DataGrid } from "@mui/x-data-grid";
import type React from "react";
import { useMemo } from "react";
import type { DataGridBaseProps } from "./types";

/**
 * DataGridBase - A reusable data grid component with consistent functionality
 * Built on top of @mui/x-data-grid with additional features:
 * - Sorting on all columns
 * - Column visibility toggle
 * - Column filtering (optional)
 * - Virtualization for performance
 * - Responsive design
 * - TypeScript-safe with generics
 * - Custom cell renderers
 * - Persistent state (localStorage)
 */
function DataGridBase<T extends GridValidRowModel>({
	rows,
	columns,
	idField = "id",
	loading = false,
	initialSortModel = [],
	onSortModelChange,
	initialHiddenColumns = [],
	onColumnVisibilityChange,
	initialFilterModel,
	onFilterModelChange,
	enableFiltering = false,
	height = 600,
	emptyStateMessage = "No rows to display",
	disableColumnMenu = false,
	disableColumnSelector = false,
	autoHeight = false,
	hidePagination = false,
}: DataGridBaseProps<T>): React.ReactElement {
	// Convert DataGridColumn to GridColDef
	const gridColumns = useMemo(() => {
		return columns.map((col) => {
			const { renderCell, ...rest } = col;

			// If custom renderCell is provided, adapt it to MUI's format
			if (renderCell) {
				return {
					...rest,
					renderCell: (params: { row: T; value: unknown }) => {
						return renderCell({ row: params.row, value: params.value });
					},
				} as GridColDef<T>;
			}

			return rest as GridColDef<T>;
		});
	}, [columns]);

	// Initialize column visibility model
	const columnVisibilityModel = useMemo(() => {
		const model: Record<string, boolean> = {};
		for (const field of initialHiddenColumns) {
			model[field] = false;
		}
		return model;
	}, [initialHiddenColumns]);

	return (
		<Box
			sx={{
				height: autoHeight ? "auto" : height,
				width: "100%",
			}}
		>
			<DataGrid
				rows={rows}
				columns={gridColumns}
				getRowId={(row) => row[idField]}
				loading={loading}
				initialState={{
					sorting: {
						sortModel: initialSortModel,
					},
					filter: initialFilterModel
						? {
								filterModel: initialFilterModel,
							}
						: undefined,
				}}
				onSortModelChange={onSortModelChange}
				onFilterModelChange={onFilterModelChange}
				columnVisibilityModel={columnVisibilityModel}
				onColumnVisibilityModelChange={(newModel) => {
					if (onColumnVisibilityChange) {
						const hiddenColumns = Object.entries(newModel)
							.filter(([, visible]) => !visible)
							.map(([field]) => field);
						onColumnVisibilityChange(hiddenColumns);
					}
				}}
				disableColumnMenu={disableColumnMenu}
				disableColumnSelector={disableColumnSelector}
				disableColumnFilter={!enableFiltering}
				autoHeight={autoHeight}
				pageSizeOptions={[10, 25, 50, 100]}
				disableRowSelectionOnClick
				hideFooter={hidePagination}
				sx={{
					"& .MuiDataGrid-cell": {
						display: "flex",
						alignItems: "center",
					},
					"& .MuiDataGrid-cell:focus": {
						outline: "none",
					},
					"& .MuiDataGrid-row:hover": {
						cursor: "pointer",
					},
				}}
				localeText={{
					noRowsLabel: emptyStateMessage,
				}}
			/>
		</Box>
	);
}

export default DataGridBase;
