import type {
	GridColDef,
	GridFilterModel,
	GridSortModel,
	GridValidRowModel,
} from "@mui/x-data-grid";
import type React from "react";

/**
 * Column definition for DataGrid
 * Extends MUI's GridColDef with additional type safety
 */
export interface DataGridColumn<T extends GridValidRowModel = GridValidRowModel>
	extends Omit<GridColDef<T>, "renderCell"> {
	field: string;
	headerName: string;
	width?: number;
	sortable?: boolean;
	hideable?: boolean;
	renderCell?: (params: { row: T; value: unknown }) => React.ReactNode;
}

/**
 * Props for DataGridBase component
 */
export interface DataGridBaseProps<T extends GridValidRowModel> {
	/** Array of rows to display */
	rows: T[];
	/** Array of column definitions */
	columns: DataGridColumn<T>[];
	/** Unique identifier field name for rows */
	idField?: string;
	/** Loading state */
	loading?: boolean;
	/** Initial sort model */
	initialSortModel?: GridSortModel;
	/** Callback when sort changes */
	onSortModelChange?: (model: GridSortModel) => void;
	/** Initial hidden columns */
	initialHiddenColumns?: string[];
	/** Callback when column visibility changes */
	onColumnVisibilityChange?: (hiddenColumns: string[]) => void;
	/** Initial filter model */
	initialFilterModel?: GridFilterModel;
	/** Callback when filter changes */
	onFilterModelChange?: (model: GridFilterModel) => void;
	/** Enable column filtering (default: false) */
	enableFiltering?: boolean;
	/** Custom height for the grid */
	height?: number | string;
	/** Custom empty state message */
	emptyStateMessage?: string;
	/** Disable column menu (default: false) */
	disableColumnMenu?: boolean;
	/** Auto height (default: false) */
	autoHeight?: boolean;
	/** Hide pagination (default: false) */
	hidePagination?: boolean;
}

/**
 * Column visibility state
 */
export interface ColumnVisibilityModel {
	[field: string]: boolean;
}

/**
 * Persisted grid state in localStorage
 */
export interface PersistedGridState {
	sortModel?: GridSortModel;
	columnVisibilityModel?: ColumnVisibilityModel;
	columnOrder?: string[];
	filterModel?: GridFilterModel;
}
