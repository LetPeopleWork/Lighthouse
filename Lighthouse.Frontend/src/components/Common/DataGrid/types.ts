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
	/** Unique identifier for localStorage persistence of column visibility. Required. Use format: 'feature-grid', 'team-members-grid', etc. */
	storageKey: string;
	/** Unique identifier field name for rows */
	idField?: string;
	/** Loading state */
	loading?: boolean;
	/** Initial sort model */
	initialSortModel?: GridSortModel;
	/** Custom empty state message */
	emptyStateMessage?: string;
	/** Hide pagination (default: true) */
	hidePagination?: boolean;
	/** Enable CSV export functionality (default: false) - Premium Feature */
	enableExport?: boolean;
	/** Custom filename for CSV export (without extension) */
	exportFileName?: string;
	/** Whether the user can reorder columns using the Column Order dialog (default: true) */
	allowColumnReorder?: boolean;
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
	/** Column widths in pixels keyed by columns' field names */
	columnWidths?: { [field: string]: number };
	filterModel?: GridFilterModel;
}
