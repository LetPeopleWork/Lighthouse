import type { GridColDef, GridValidRowModel } from "@mui/x-data-grid";
import type { DataGridColumn } from "./types";

/**
 * How a column behaves when the table is narrower than its columns want.
 * - text: shares the leftover width and shrinks down to its minimum, cutting long text with an ellipsis.
 * - content: keeps its declared width and is never cut (numbers, dates, states).
 * - actions: like content, and pinned to the right edge so the row actions never scroll out of view.
 */
export type ColumnFit =
	| { kind: "text"; minWidth?: number }
	| { kind: "content" }
	| { kind: "actions" };

type ColumnFitKind = ColumnFit["kind"];

/** The narrowest a text column gets when it declares no minimum of its own. */
export const DEFAULT_TEXT_MIN_WIDTH = 120;

/** The narrowest a Name column gets, so a person can still tell rows apart. */
export const NAME_MIN_WIDTH = 160;

/** The width the community grid gives a column that declares none. */
const GRID_DEFAULT_WIDTH = 100;

/** The class that keeps the actions column at the right edge while the rest scrolls. */
export const PINNED_ACTIONS_CLASS = "lighthouse-pinned-actions";

/** The column props the fit decides; everything else of a column passes through unchanged. */
export type FittedColumn = Pick<
	GridColDef,
	| "flex"
	| "width"
	| "minWidth"
	| "resizable"
	| "cellClassName"
	| "headerClassName"
>;

/** What the fit reads of a column: its declared sizes and its fit choice. */
type SizedColumn = Pick<DataGridColumn, "width" | "minWidth" | "flex" | "fit">;

type FitRule = (
	column: SizedColumn,
	fit: ColumnFit,
	storedWidth: number | undefined,
) => FittedColumn;

const declaredWidthOf = (column: SizedColumn): number =>
	column.width ?? GRID_DEFAULT_WIDTH;

const fitText: FitRule = (column, fit, storedWidth) => {
	const declaredMinimum = fit.kind === "text" ? fit.minWidth : undefined;
	const minWidth = declaredMinimum ?? column.minWidth ?? DEFAULT_TEXT_MIN_WIDTH;
	if (storedWidth === undefined) {
		return { flex: column.flex ?? 1, width: undefined, minWidth };
	}
	return { flex: undefined, width: storedWidth, minWidth };
};

const fitContent: FitRule = (column, _fit, storedWidth) => {
	const declared = declaredWidthOf(column);
	return {
		flex: undefined,
		width: Math.max(declared, storedWidth ?? declared),
		minWidth: declared,
	};
};

const fitActions: FitRule = (column) => {
	const declared = declaredWidthOf(column);
	return {
		flex: undefined,
		width: declared,
		minWidth: declared,
		resizable: false,
		cellClassName: PINNED_ACTIONS_CLASS,
		headerClassName: PINNED_ACTIONS_CLASS,
	};
};

const FIT_RULES: Record<ColumnFitKind, FitRule> = {
	text: fitText,
	content: fitContent,
	actions: fitActions,
};

/** A column that names no fit is text when it shares width (flex) and content otherwise. */
export const fitOf = (column: SizedColumn): ColumnFit =>
	column.fit ??
	(column.flex === undefined ? { kind: "content" } : { kind: "text" });

/**
 * The width props of a column, given the width the person stored for it, if any. A text column
 * shrinks to its minimum unless the person set its width; content and actions columns are never
 * narrower than declared, and the actions column ignores a stored width and cannot be resized.
 */
export const fitColumn = <T extends GridValidRowModel>(
	column: DataGridColumn<T>,
	storedWidth: number | undefined,
): FittedColumn => {
	const fit = fitOf(column);
	return FIT_RULES[fit.kind](column, fit, storedWidth);
};
