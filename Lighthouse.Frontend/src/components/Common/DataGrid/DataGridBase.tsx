import { Box } from "@mui/material";
import type {
	GridColDef,
	GridColumnResizeParams,
	GridColumnVisibilityModel,
	GridRenderCellParams,
	GridValidRowModel,
} from "@mui/x-data-grid";
import { DataGrid, GridApiContext, useGridApiRef } from "@mui/x-data-grid";
import type React from "react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import ColumnOrderDialog from "./ColumnOrderDialog";
import DataGridToolbar from "./DataGridToolbar";
import {
	useColumnOrder,
	useColumnWidths,
	usePersistedGridState,
} from "./hooks";
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
	allowColumnReorder = true,
	toolbarActions,
}: Readonly<DataGridBaseProps<T>>): React.ReactElement {
	// Check license status for premium features
	const { licenseStatus } = useLicenseRestrictions();
	const canUsePremiumFeatures = licenseStatus?.canUsePremiumFeatures ?? false;

	// Create API ref to access DataGrid API
	const apiRef = useGridApiRef();

	// Generate localStorage key for this grid's persisted state (visibility, order, widths, sort)
	const storageKeyForState = `lighthouse:datagrid:${storageKey}:state`;

	// Persisted state hook for the DataGrid
	const {
		state: persistedState,

		updateState,
		clearState,
	} = usePersistedGridState(storageKeyForState);

	const [columnVisibilityModel, setColumnVisibilityModel] =
		useState<GridColumnVisibilityModel>(() => {
			if (persistedState?.columnVisibilityModel)
				return persistedState.columnVisibilityModel;
			return {};
		});

	// Sanitize persisted visibility to ensure non-hideable columns are always visible.
	useEffect(() => {
		if (!columnVisibilityModel) return;
		const sanitized = { ...columnVisibilityModel } as GridColumnVisibilityModel;
		let changed = false;
		for (const col of columns) {
			if (col.hideable === false && sanitized[col.field] === false) {
				sanitized[col.field] = true;
				changed = true;
			}
		}
		if (changed) {
			setColumnVisibilityModel(sanitized);
			updateState((prev) => ({ ...prev, columnVisibilityModel: sanitized }));
		}
	}, [columns, columnVisibilityModel, updateState]);

	// Initialize column order & widths using persisted state or defaults
	const initialColumnOrder =
		persistedState?.columnOrder ?? columns.map((c) => c.field);
	const {
		columnOrder,
		reset: resetColumnOrder,
		setColumnOrder,
	} = useColumnOrder(initialColumnOrder);

	const defaultInitialWidths = columns.reduce<Record<string, number>>(
		(acc, c) => {
			// Only include numeric widths for columns that don't use flex, so flex columns remain responsive
			if (typeof c.width === "number" && c.flex === undefined)
				acc[c.field] = c.width;
			return acc;
		},
		{},
	);
	const {
		columnWidths,
		setColumnWidth,
		setAllWidths,
		reset: resetColumnWidths,
	} = useColumnWidths(defaultInitialWidths);

	const [isColumnOrderDialogOpen, setIsColumnOrderDialogOpen] = useState(false);
	const resizeTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

	// Apply persisted column widths on mount (only override columns that user resized before)
	useEffect(() => {
		if (persistedState?.columnWidths && setAllWidths) {
			setAllWidths(persistedState.columnWidths);
		}
	}, [persistedState?.columnWidths, setAllWidths]);

	// Clear any pending debounce timers when the component unmounts
	useEffect(() => {
		return () => {
			if (resizeTimeoutRef.current)
				globalThis.clearTimeout(resizeTimeoutRef.current);
		};
	}, []);

	// Convert DataGridColumn to GridColDef
	const gridColumns = useMemo(() => {
		return columns.map((col) => {
			const { renderCell, ...rest } = col;

			// If custom renderCell is provided, adapt it to MUI's format
			const persistedWidth = (columnWidths as Record<string, number>)[
				rest.field
			];
			const width = persistedWidth ?? rest.width;
			if (renderCell) {
				const colDef: GridColDef<T> = {
					...rest,
					width,
					renderCell: (params: GridRenderCellParams<T>) => {
						return renderCell({ row: params.row, value: params.value });
					},
				};
				if (persistedWidth !== undefined) {
					// Remove flex to avoid MUI rebalancing the width for this column after persist
					(colDef as Partial<GridColDef<T>>).flex = undefined;
				}
				return colDef as GridColDef<T>;
			}

			const colDef: GridColDef<T> = { ...rest, width } as GridColDef<T>;
			if (persistedWidth !== undefined) {
				(colDef as Partial<GridColDef<T>>).flex = undefined;
			}
			return colDef;
		});
	}, [columns, columnWidths]);

	return (
		<Box
			sx={{
				height: "auto",
				width: "100%",
			}}
		>
			<GridApiContext.Provider value={apiRef}>
				<DataGridToolbar
					canUsePremiumFeatures={canUsePremiumFeatures}
					enableExport={enableExport}
					exportFileName={exportFileName}
					onResetLayout={() => {
						// Clear persisted state and reset local state
						clearState();
						// Reset to our default initial widths (don't re-apply persisted widths)
						if (setAllWidths) setAllWidths(defaultInitialWidths);
						resetColumnWidths();
						resetColumnOrder();
						setColumnVisibilityModel({});
					}}
					onOpenColumnOrder={() => setIsColumnOrderDialogOpen(true)}
					allowColumnReorder={allowColumnReorder}
					customActions={toolbarActions}
				/>
				<ColumnOrderDialog
					open={isColumnOrderDialogOpen}
					onClose={() => setIsColumnOrderDialogOpen(false)}
					columns={gridColumns}
					columnOrder={columnOrder}
					onSave={(order: string[]) => {
						setColumnOrder(order);
						updateState((prev) => ({ ...prev, columnOrder: order }));
					}}
				/>
				<DataGrid
					apiRef={apiRef}
					rows={rows}
					columns={
						// Apply ordering based on columnOrder state
						((): GridColDef<T>[] => {
							const map = new Map(gridColumns.map((c) => [c.field, c]));
							const ordered: GridColDef<T>[] = [];
							const order = columnOrder ?? gridColumns.map((c) => c.field);
							const orderSet = new Set(order);
							for (const f of order) {
								const col = map.get(f);
								if (col) ordered.push(col);
							}
							// Append any columns missing in order
							for (const c of gridColumns) {
								if (!orderSet.has(c.field)) ordered.push(c);
							}
							return ordered;
						})()
					}
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
					disableColumnResize={false}
					columnVisibilityModel={columnVisibilityModel}
					onColumnVisibilityModelChange={(newModel) => {
						// Prevent non-hideable columns from being hidden
						const sanitizedModel = { ...newModel } as GridColumnVisibilityModel;
						for (const col of columns) {
							if (
								col.hideable === false &&
								sanitizedModel[col.field] === false
							) {
								// Force visible
								sanitizedModel[col.field] = true;
							}
						}
						setColumnVisibilityModel(sanitizedModel);
						updateState((prev) => ({
							...prev,
							columnVisibilityModel: sanitizedModel,
						}));
					}}
					onColumnOrderChange={
						allowColumnReorder
							? () => {
									const api = apiRef.current;
									let orderedFields: string[] = [];
									if (api) {
										try {
											const all = api.getAllColumns();
											orderedFields = all.map((c) => c.field);
										} catch (err) {
											console.warn(
												"Failed to getAllColumns for column order, falling back to visible columns",
												err,
											);
											try {
												orderedFields = api
													.getVisibleColumns()
													.map((c) => c.field);
											} catch (error_) {
												console.warn(
													"Failed to getVisibleColumns as fallback",
													error_,
												);
												orderedFields = columns.map((c) => c.field);
											}
										}
									} else {
										orderedFields = columns.map((c) => c.field);
									}
									setColumnOrder(orderedFields);
									updateState((prev) => ({
										...prev,
										columnOrder: orderedFields,
									}));
								}
							: undefined
					}
					onColumnResize={(params: GridColumnResizeParams) => {
						try {
							const field = params.colDef?.field;
							const width = params.width;
							if (typeof field === "string" && typeof width === "number") {
								// update temporary width for UI responsiveness; persist on commit
								setColumnWidth(field, width);
							}
							// Debounce the persisted save so we only write final widths once user stops resizing
							const delay = 250; // ms
							if (resizeTimeoutRef.current) {
								globalThis.clearTimeout(resizeTimeoutRef.current);
							}
							resizeTimeoutRef.current = globalThis.setTimeout(() => {
								try {
									const api = apiRef.current;
									let cols: Array<{ field: string; width?: number }> = [];
									if (api) {
										try {
											cols = api.getAllColumns();
										} catch {
											try {
												cols = api.getVisibleColumns();
											} catch {
												cols = [];
											}
										}
									}
									const nextWidths: { [field: string]: number } = {};
									for (const c of cols) {
										const f = c.field;
										const w = c.width;
										if (typeof w === "number") {
											nextWidths[f] = w;
											setColumnWidth(f, w);
										}
									}
									updateState((prev) => ({
										...prev,
										columnWidths: nextWidths,
									}));
								} catch (error) {
									console.warn(
										"Failed to persist column widths after resize commit",
										error,
									);
								}
							}, delay);
						} catch (error) {
							console.warn("Failed to handle column resize", error);
						}
					}}
					onSortModelChange={(model) => {
						updateState((prev) => ({ ...prev, sortModel: model }));
					}}
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
