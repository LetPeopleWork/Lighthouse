import type {
	GridColDef,
	GridFilterModel,
	GridRowId,
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
	/** Value getter function for accessing nested or computed values for sorting and filtering */
	valueGetter?: (value: unknown, row: T) => unknown;
	/** Custom sort comparator function for defining sort behavior */
	sortComparator?: (
		v1: unknown,
		v2: unknown,
		cellParams1: unknown,
		cellParams2: unknown,
	) => number;
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
	/** Lets a grid know a column sort is deciding its order, so it can withdraw the row actions that mean nothing under one. */
	onSortModelChange?: (sortModel: GridSortModel) => void;
	/** Custom empty state message */
	emptyStateMessage?: string;
	/** Hide pagination (default: true) */
	hidePagination?: boolean;
	/** Enable CSV export functionality (default: false) - Premium Feature */
	enableExport?: boolean;
	/** Custom filename for CSV export (without extension) */
	exportFileName?: string;
	/** The whole exported table, built by the caller. Given one, the toolbar exports it verbatim. */
	exportTable?: (orderedRowIds: GridRowId[]) => DataGridExportTable;
	/** Whether the user can reorder columns using the Column Order dialog (default: true) */
	allowColumnReorder?: boolean;
	/** Custom actions to display in the toolbar */
	toolbarActions?: React.ReactNode;
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

/**
 * Everything a grid puts in an exported file, built by whoever owns the rows. Half of what a reader
 * sees in a cell is drawn by a renderer with no field behind it, so a grid that has any such column
 * has to say what its file contains rather than let the toolbar read it back off the screen.
 */
export interface DataGridExportTable {
	headers: string[];
	rows: string[][];
}

/**
 * Props for DataGridToolbar component
 */
export interface DataGridToolbarProps {
	/** Whether user has premium features available */
	canUsePremiumFeatures?: boolean;
	/** Whether export functionality is enabled */
	enableExport?: boolean;
	/** Custom filename for CSV export (without extension) */
	exportFileName?: string;
	/** The whole exported table, built by the caller. Given one, the toolbar exports it verbatim. */
	exportTable?: (orderedRowIds: GridRowId[]) => DataGridExportTable;
	/** Reset layout handler */
	onResetLayout?: () => void;
	/** Open column order dialog */
	onOpenColumnOrder?: () => void;
	/** Whether user may reorder columns */
	allowColumnReorder?: boolean;
	/** Custom actions to display in the toolbar */
	customActions?: React.ReactNode;
}
