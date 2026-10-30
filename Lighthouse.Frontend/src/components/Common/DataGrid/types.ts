import type {
	GridColDef,
	GridFilterModel,
	GridRowId,
	GridRowSpacing,
	GridSortModel,
	GridValidRowModel,
	DataGridProps as MuiDataGridProps,
} from "@mui/x-data-grid";
import type React from "react";

/** A row's place in the whole list as sorted and filtered, whichever page it is on. */
export interface RowInWholeList {
	index: number;
	isLastShown: boolean;
}

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
	/**
	 * The choices a `singleSelect` column offers in its filter dropdown. Restated here because
	 * GridColDef is a union and this key lives only on two of its members, so `Omit` over the union
	 * drops it. Strings only: the object form would let a column's displayed label and its stored
	 * value differ, and every consumer here reads one string for the cell, the filter and the export.
	 */
	valueOptions?: string[];
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
	/** Replaces parts of the grid, such as how a row renders. */
	slots?: MuiDataGridProps<T>["slots"];
	/** Space to leave above or below a row, which the grid counts when it lays the rows out. */
	getRowSpacing?: (row: RowInWholeList) => GridRowSpacing;
	/**
	 * Which columns start hidden. Only what the user changes on top of it is remembered, so a later
	 * change to these defaults still reaches every column the user never touched.
	 */
	defaultColumnVisibilityModel?: ColumnVisibilityModel;
	/** Puts a checkbox on every row and one over the header; without it the grid offers none. */
	checkboxSelection?: RowPicking;
}

/** Which rows are picked is held by the caller, so it can act on them and decide when the picks are cleared. */
export interface RowPicking {
	selectedIds: readonly GridRowId[];
	onSelectionChange: (selectedIds: GridRowId[]) => void;
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
