import { Box } from "@mui/material";
import type {
	GridColDef,
	GridColumnResizeParams,
	GridColumnVisibilityModel,
	GridRenderCellParams,
	GridRowSelectionModel,
	GridRowSpacingParams,
	GridValidRowModel,
} from "@mui/x-data-grid";
import {
	DataGrid,
	GridApiContext,
	gridExpandedRowCountSelector,
	gridPageCountSelector,
	gridPageSelector,
	gridPaginationModelSelector,
	useGridApiRef,
} from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import ColumnOrderDialog from "./ColumnOrderDialog";
import { fitColumn, fitOf, PINNED_ACTIONS_CLASS } from "./columnFit";
import DataGridToolbar from "./DataGridToolbar";
import {
	useColumnOrder,
	useColumnWidths,
	usePersistedGridState,
} from "./hooks";
import { PAGE_SIZE, RowRangeFooter } from "./RowRangeFooter";
import TruncatedCell from "./TruncatedCell";
import type {
	ColumnVisibilityModel,
	DataGridBaseProps,
	DataGridColumn,
	RowPicking,
} from "./types";

/**
 * The user's overrides are laid over the defaults, so a column they never touched always follows
 * the current defaults. Without defaults the overrides are the whole model, as before.
 */
function withOverrides(
	defaults: ColumnVisibilityModel | undefined,
	overrides: ColumnVisibilityModel | undefined,
): GridColumnVisibilityModel {
	return { ...defaults, ...overrides };
}

/**
 * Only the columns the user set differently from the defaults are remembered; the grid treats a
 * column missing from either model as shown. Without defaults the whole model is remembered.
 */
function overridesOf(
	model: GridColumnVisibilityModel,
	defaults: ColumnVisibilityModel | undefined,
): ColumnVisibilityModel {
	if (defaults === undefined) return model;
	const overrides: ColumnVisibilityModel = {};
	const fields = new Set([...Object.keys(model), ...Object.keys(defaults)]);
	for (const field of fields) {
		const shown = model[field] ?? true;
		if (shown !== (defaults[field] ?? true)) overrides[field] = shown;
	}
	return overrides;
}

function hidesUnhideableColumn<T extends GridValidRowModel>(
	model: GridColumnVisibilityModel,
	columns: DataGridColumn<T>[],
): boolean {
	return columns.some(
		(col) => col.hideable === false && model[col.field] === false,
	);
}

function withUnhideableColumnsShown<T extends GridValidRowModel>(
	model: GridColumnVisibilityModel,
	columns: DataGridColumn<T>[],
): GridColumnVisibilityModel {
	const sanitized = { ...model };
	for (const col of columns) {
		if (col.hideable === false && sanitized[col.field] === false) {
			sanitized[col.field] = true;
		}
	}
	return sanitized;
}

/**
 * A saved column order knows nothing of columns added since it was saved. Each of those goes right
 * after the column declared before it, or first when it is declared first, rather than at the end,
 * where a leading column such as a row number would make no sense.
 */
function withNewColumnsWhereDeclared(
	savedOrder: string[],
	declaredOrder: string[],
): string[] {
	const merged = [...savedOrder];
	let afterPrevious = 0;
	for (const field of declaredOrder) {
		const saved = merged.indexOf(field);
		if (saved === -1) {
			merged.splice(afterPrevious, 0, field);
			afterPrevious += 1;
		} else {
			afterPrevious = saved + 1;
		}
	}
	return merged;
}

/** The pinned actions column stays at the right edge, so it is ordered last whatever order is saved. */
function withActionsLast<T extends GridValidRowModel>(
	order: string[],
	columns: DataGridColumn<T>[],
): string[] {
	const actions = new Set(
		columns
			.filter((column) => fitOf(column).kind === "actions")
			.map((column) => column.field),
	);
	return [
		...order.filter((field) => !actions.has(field)),
		...order.filter((field) => actions.has(field)),
	];
}

/** The text a cell holds, for its tooltip; a value that is not plain text gives none. */
function textOf(value: unknown): string {
	if (typeof value === "string") return value;
	if (typeof value === "number" || typeof value === "boolean")
		return String(value);
	return "";
}

const COLUMN_BUFFER_PX = 10_000;

/** Column types the grid draws itself with more than text, so their cells are left to it. */
const TYPES_DRAWN_BY_THE_GRID = new Set(["boolean", "actions", "custom"]);

function toGridColumn<T extends GridValidRowModel>(
	column: DataGridColumn<T>,
	storedWidth: number | undefined,
): GridColDef<T> {
	const { renderCell, fit: _fit, ...rest } = column;
	const fitted = fitColumn(column, storedWidth);
	const isText = fitOf(column).kind === "text";
	const drawnByTheGrid =
		renderCell === undefined && TYPES_DRAWN_BY_THE_GRID.has(column.type ?? "");

	let render: GridColDef<T>["renderCell"];
	if (isText && !drawnByTheGrid) {
		render = (params: GridRenderCellParams<T>) => {
			const shown = textOf(params.formattedValue) || textOf(params.value);
			return (
				<TruncatedCell fullText={shown} hasFocus={params.hasFocus}>
					{renderCell
						? renderCell({ row: params.row, value: params.value })
						: shown}
				</TruncatedCell>
			);
		};
	} else if (renderCell) {
		render = (params: GridRenderCellParams<T>) =>
			renderCell({ row: params.row, value: params.value });
	}

	return {
		...rest,
		...fitted,
		resizable: fitted.resizable ?? rest.resizable,
		...(render === undefined ? {} : { renderCell: render }),
	} as GridColDef<T>;
}

type GridApiRef = ReturnType<typeof useGridApiRef>;

/**
 * When the rows shrink under an open page that no longer exists, the grid moves off it only a tick
 * later, and until then it shows the list's last row alone under a range that already names the page
 * it is about to show. Moving at once, as the row count changes, never lets that frame be drawn.
 */
function useMovingOffAPageThatIsGone(apiRef: GridApiRef) {
	useEffect(
		() =>
			apiRef.current?.subscribeEvent("rowCountChange", () => {
				const lastPage = Math.max(0, gridPageCountSelector(apiRef) - 1);
				if (gridPageSelector(apiRef) > lastPage)
					apiRef.current?.setPage(lastPage);
			}),
		[apiRef],
	);
}

/**
 * The grid counts a row's spacing from the top of the open page and calls a row last when it ends
 * that page. Space kept for something placed in the whole list must follow the row's place there.
 */
function useSpacingInWholeList(
	apiRef: GridApiRef,
	getRowSpacing: DataGridBaseProps<GridValidRowModel>["getRowSpacing"],
) {
	return useMemo(() => {
		if (getRowSpacing === undefined) return undefined;
		return ({ indexRelativeToCurrentPage }: GridRowSpacingParams) => {
			const { page, pageSize } = gridPaginationModelSelector(apiRef);
			const index = page * pageSize + indexRelativeToCurrentPage;
			return getRowSpacing({
				index,
				isLastShown: index === gridExpandedRowCountSelector(apiRef) - 1,
			});
		};
	}, [apiRef, getRowSpacing]);
}

/** Hands the caller a way to read the rows' order when it acts on them, rather than following every sort. */
function useTableOrderReader(
	apiRef: GridApiRef,
	tableOrder: RowPicking["tableOrder"],
) {
	useEffect(() => {
		if (tableOrder === undefined) return;
		tableOrder.current = () => apiRef.current?.getSortedRowIds() ?? [];
		return () => {
			tableOrder.current = undefined;
		};
	}, [apiRef, tableOrder]);
}

/**
 * The grid's checkbox props, or none at all when the caller does not pick rows. The picks are always named one by
 * one, never as "every row but these", so the caller reads them without knowing which rows the grid holds.
 */
function useRowPicking(picking: RowPicking | undefined) {
	const selectedIds = picking?.selectedIds;
	const onSelectionChange = picking?.onSelectionChange;
	return useMemo(() => {
		if (selectedIds === undefined || onSelectionChange === undefined) return {};
		const rowSelectionModel: GridRowSelectionModel = {
			type: "include",
			ids: new Set(selectedIds),
		};
		return {
			checkboxSelection: true,
			disableRowSelectionExcludeModel: true,
			rowSelectionModel,
			onRowSelectionModelChange: (model: GridRowSelectionModel) =>
				onSelectionChange([...model.ids]),
		};
	}, [selectedIds, onSelectionChange]);
}

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
	onSortModelChange,
	emptyStateMessage = "No rows to display",
	enableExport = false,
	exportFileName,
	exportTable,
	allowColumnReorder = true,
	toolbarActions,
	slots,
	getRowSpacing,
	defaultColumnVisibilityModel,
	checkboxSelection,
}: Readonly<DataGridBaseProps<T>>): React.ReactElement {
	// Check license status for premium features
	const { licenseStatus } = useLicenseRestrictions();
	const canUsePremiumFeatures = licenseStatus?.canUsePremiumFeatures ?? false;

	// Create API ref to access DataGrid API
	const apiRef = useGridApiRef();
	const spacingInWholeList = useSpacingInWholeList(apiRef, getRowSpacing);
	const rowPicking = useRowPicking(checkboxSelection);

	// Generate localStorage key for this grid's persisted state (visibility, order, widths, sort)
	const storageKeyForState = `lighthouse:datagrid:${storageKey}:state`;

	// Persisted state hook for the DataGrid
	const {
		state: persistedState,

		updateState,
		clearState,
	} = usePersistedGridState(storageKeyForState);

	// Only the user's overrides are held, so defaults that change while the grid is shown (a dialog
	// whose rows arrive after it opened) still reach every column the user never touched.
	const [visibilityOverrides, setVisibilityOverrides] = useState<
		ColumnVisibilityModel | undefined
	>(() => persistedState?.columnVisibilityModel);

	const columnVisibilityModel = useMemo(
		() => withOverrides(defaultColumnVisibilityModel, visibilityOverrides),
		[defaultColumnVisibilityModel, visibilityOverrides],
	);

	const applyVisibility = useCallback(
		(model: GridColumnVisibilityModel) => {
			const overrides = overridesOf(model, defaultColumnVisibilityModel);
			setVisibilityOverrides(overrides);
			updateState((prev) => ({ ...prev, columnVisibilityModel: overrides }));
		},
		[updateState, defaultColumnVisibilityModel],
	);

	// Sanitize persisted visibility to ensure non-hideable columns are always visible.
	useEffect(() => {
		if (!hidesUnhideableColumn(columnVisibilityModel, columns)) return;
		applyVisibility(withUnhideableColumnsShown(columnVisibilityModel, columns));
	}, [columns, columnVisibilityModel, applyVisibility]);

	// Initialize column order & widths using persisted state or defaults
	const initialColumnOrder =
		persistedState?.columnOrder ?? columns.map((c) => c.field);
	const {
		columnOrder,
		reset: resetColumnOrder,
		setColumnOrder,
	} = useColumnOrder(initialColumnOrder);

	// Only the widths the person set by resizing are held; every other column fits the table.
	const { columnWidths, setColumnWidth, setAllWidths } = useColumnWidths(
		persistedState?.columnWidths ?? {},
	);

	const [isColumnOrderDialogOpen, setIsColumnOrderDialogOpen] = useState(false);
	const resizeTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
	const resizedWidthsRef = useRef<Record<string, number>>({});

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

	useMovingOffAPageThatIsGone(apiRef);
	useTableOrderReader(apiRef, checkboxSelection?.tableOrder);

	// Convert DataGridColumn to GridColDef
	const gridColumns = useMemo(
		() =>
			columns.map((column) => toGridColumn(column, columnWidths[column.field])),
		[columns, columnWidths],
	);

	const orderedColumns = useMemo(() => {
		const byField = new Map(gridColumns.map((c) => [c.field, c]));
		const declaredOrder = [...byField.keys()];
		return withActionsLast(
			withNewColumnsWhereDeclared(columnOrder ?? declaredOrder, declaredOrder),
			columns,
		).flatMap((field) => byField.get(field) ?? []);
	}, [gridColumns, columnOrder, columns]);

	const shownColumnOrder = useMemo(
		() => orderedColumns.map((c) => c.field),
		[orderedColumns],
	);

	const gridSlots = useMemo(
		() => ({ ...slots, footer: RowRangeFooter }),
		[slots],
	);

	return (
		<Box
			sx={{
				height: "auto",
				width: "100%",
				// Lets the table scroll sideways inside its own box instead of widening the page.
				minWidth: 0,
			}}
		>
			<GridApiContext.Provider value={apiRef}>
				<DataGridToolbar
					canUsePremiumFeatures={canUsePremiumFeatures}
					enableExport={enableExport}
					exportFileName={exportFileName}
					exportTable={exportTable}
					onResetLayout={() => {
						// Clear persisted state and reset local state
						clearState();
						// Back to the fitted layout: no column keeps a width the person set
						setAllWidths({});
						resizedWidthsRef.current = {};
						resetColumnOrder();
						setVisibilityOverrides(undefined);
					}}
					onOpenColumnOrder={() => setIsColumnOrderDialogOpen(true)}
					allowColumnReorder={allowColumnReorder}
					customActions={toolbarActions}
				/>
				<ColumnOrderDialog
					open={isColumnOrderDialogOpen}
					onClose={() => setIsColumnOrderDialogOpen(false)}
					columns={gridColumns}
					columnOrder={shownColumnOrder}
					onSave={(order: string[]) => {
						setColumnOrder(order);
						updateState((prev) => ({ ...prev, columnOrder: order }));
					}}
				/>
				<DataGrid
					apiRef={apiRef}
					rows={rows}
					columns={orderedColumns}
					getRowId={(row) => row[idField]}
					loading={loading}
					initialState={{
						sorting: {
							sortModel: initialSortModel,
						},
					}}
					pageSizeOptions={[PAGE_SIZE]}
					disableRowSelectionOnClick
					hideFooterSelectedRowCount
					{...rowPicking}
					getRowHeight={() => "auto"}
					slots={gridSlots}
					getRowSpacing={spacingInWholeList}
					disableColumnResize={false}
					// Every column is drawn, never virtualised away: a cell that is not drawn cannot stay
					// pinned at the right edge. The shared tables have few columns and page their rows.
					columnBufferPx={COLUMN_BUFFER_PX}
					columnVisibilityModel={columnVisibilityModel}
					onColumnVisibilityModelChange={(newModel) =>
						applyVisibility(withUnhideableColumnsShown(newModel, columns))
					}
					onColumnOrderChange={
						allowColumnReorder
							? () => {
									const api = apiRef.current;
									let orderedFields: string[] = [];
									if (api) {
										try {
											const all = api.getAllColumns();
											orderedFields = all.map((c) => c.field);
										} catch (error) {
											console.warn(
												"Failed to getAllColumns for column order, falling back to visible columns",
												error,
											);
											try {
												orderedFields = api
													.getVisibleColumns()
													.map((c) => c.field);
											} catch (error) {
												console.warn(
													"Failed to getVisibleColumns as fallback",
													error,
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
								resizedWidthsRef.current = {
									...resizedWidthsRef.current,
									[field]: width,
								};
							}
							// Debounce the persisted save so we only write final widths once user stops resizing
							const delay = 250; // ms
							if (resizeTimeoutRef.current) {
								globalThis.clearTimeout(resizeTimeoutRef.current);
							}
							resizeTimeoutRef.current = globalThis.setTimeout(() => {
								try {
									// Storing every column's width would take all of them out of the fit, so
									// only the columns the person resized are stored.
									const resized = resizedWidthsRef.current;
									resizedWidthsRef.current = {};
									updateState((prev) => ({
										...prev,
										columnWidths: { ...prev.columnWidths, ...resized },
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
						onSortModelChange?.(model);
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
						[`& .${PINNED_ACTIONS_CLASS}`]: {
							position: "sticky",
							right: 0,
							zIndex: 3,
							backgroundColor: (theme) =>
								`var(--DataGrid-t-cell-background-pinned, ${theme.palette.background.paper})`,
							borderLeft: 1,
							borderLeftColor: "divider",
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
