import { Box } from "@mui/material";
import type {
	GridColDef,
	GridColumnVisibilityModel,
	GridValidRowModel,
} from "@mui/x-data-grid";
import { DataGrid, GridApiContext, useGridApiRef } from "@mui/x-data-grid";
import type React from "react";
import { useEffect, useMemo, useState } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import DataGridToolbar from "./DataGridToolbar";
import type { DataGridBaseProps } from "./types";

/**
 * DataGridBase - A reusable data grid component with consistent functionality
 * Built on top of @mui/x-data-grid with additional features:
 * - Sorting on all columns
 * - Column visibility toggle with localStorage persistence
 * - Column filtering (enabled by default)
 * - Virtualization for performance
 * - Responsive design
 * - TypeScript-safe with generics
 * - Custom cell renderers
 * - CSV export (Premium feature)
 */
function DataGridBase<T extends GridValidRowModel>({
	rows,
	columns,
	storageKey,
	idField = "id",
	loading = false,
	initialSortModel = [],
	emptyStateMessage = "No rows to display",
	hidePagination = true,
	enableExport = false,
	exportFileName,
}: Readonly<DataGridBaseProps<T>>): React.ReactElement {
	// Check license status for premium features
	const { licenseStatus } = useLicenseRestrictions();
	const canUsePremiumFeatures = licenseStatus?.canUsePremiumFeatures ?? false;

	// Create API ref to access DataGrid API
	const apiRef = useGridApiRef();

	// Generate localStorage key for this grid's column visibility
	const storageKeyForVisibility = `lighthouse:datagrid:${storageKey}:columnVisibility`;

	// Load initial column visibility from localStorage
	const [columnVisibilityModel, setColumnVisibilityModel] =
		useState<GridColumnVisibilityModel>(() => {
			try {
				const stored = localStorage.getItem(storageKeyForVisibility);
				if (stored) {
					return JSON.parse(stored) as GridColumnVisibilityModel;
				}
			} catch (error) {
				console.warn(
					`Failed to load column visibility from localStorage for key: ${storageKeyForVisibility}`,
					error,
				);
			}
			return {};
		});

	// Persist column visibility changes to localStorage
	useEffect(() => {
		try {
			localStorage.setItem(
				storageKeyForVisibility,
				JSON.stringify(columnVisibilityModel),
			);
		} catch (error) {
			console.warn(
				`Failed to save column visibility to localStorage for key: ${storageKeyForVisibility}`,
				error,
			);
		}
	}, [columnVisibilityModel, storageKeyForVisibility]);

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

	return (
		<Box
			sx={{
				height: "auto",
				width: "100%",
			}}
		>
			<GridApiContext.Provider value={apiRef}>
				{enableExport && (
					<DataGridToolbar
						canUsePremiumFeatures={canUsePremiumFeatures}
						enableExport={enableExport}
						exportFileName={exportFileName}
					/>
				)}
				<DataGrid
					apiRef={apiRef}
					rows={rows}
					columns={gridColumns}
					getRowId={(row) => row[idField]}
					loading={loading}
					initialState={{
						sorting: {
							sortModel: initialSortModel,
						},
					}}
					pageSizeOptions={[10, 25, 50, 100]}
					disableRowSelectionOnClick
					hideFooter={hidePagination}
					getRowHeight={() => "auto"}
					columnVisibilityModel={columnVisibilityModel}
					onColumnVisibilityModelChange={(newModel) =>
						setColumnVisibilityModel(newModel)
					}
					sx={{
						"& .MuiDataGrid-cell": {
							display: "flex",
							alignItems: "center",
							paddingTop: 1,
							paddingBottom: 1,
							whiteSpace: "normal",
							wordWrap: "break-word",
						},
						"& .MuiDataGrid-cell:focus": {
							outline: "none",
						},
						"& .MuiDataGrid-row": {
							maxHeight: "none !important",
						},
						"& .MuiDataGrid-row:hover": {
							cursor: "pointer",
						},
					}}
					localeText={{
						noRowsLabel: emptyStateMessage,
					}}
				/>
			</GridApiContext.Provider>
		</Box>
	);
}

export default DataGridBase;
