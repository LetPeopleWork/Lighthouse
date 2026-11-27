import type { GridColDef } from "@mui/x-data-grid";

/**
 * The text typed into a list's search box is one literal piece of text, so it is handed to the grid
 * as a single value; the grid's own quick filter would otherwise treat each word as its own search.
 * A blank box hands over nothing, which leaves every row in the list.
 */
export const quickSearchValues = (typed: string): string[] => {
	const trimmed = typed.trim();
	return trimmed === "" ? [] : [trimmed];
};

type ShownCellTest = (cellValue: unknown) => boolean;

/** Only text and numbers are shown as text; an object, a yes/no or a missing value shows none. */
const shownTextOf = (cellValue: unknown): string | null => {
	if (typeof cellValue === "string") return cellValue;
	if (typeof cellValue === "number") return String(cellValue);
	return null;
};

/**
 * Tests the text a cell shows against the typed text, in any letter case. The grid's own text match
 * would test an object cell as "[object Object]", so a person typing "object" would find rows.
 */
export const shownTextQuickFilter = (typed: string): ShownCellTest | null => {
	const needle = typed.trim().toLowerCase();
	if (needle === "") return null;
	return (cellValue) =>
		shownTextOf(cellValue)?.toLowerCase().includes(needle) ?? false;
};

/** Column types whose cells are not plain text, which keep the grid's own handling. */
const NOT_PLAIN_TEXT = new Set(["boolean", "actions", "custom"]);

/**
 * The same matcher for a grid column, tested against the text the column shows (its formatted
 * value) rather than the value behind it. A column that brings its own quick filter keeps it.
 */
export const withShownTextQuickFilter = <C extends GridColDef>(
	column: C,
): C => {
	if (NOT_PLAIN_TEXT.has(column.type ?? "")) return column;
	if (column.getApplyQuickFilterFn !== undefined) return column;
	const getApplyQuickFilterFn: GridColDef["getApplyQuickFilterFn"] = (
		typed,
	) => {
		const test = shownTextQuickFilter(typed);
		if (test === null) return null;
		return (_cellValue, row, gridColumn, apiRef) =>
			test(apiRef.current.getRowFormattedValue(row, gridColumn));
	};
	return { ...column, getApplyQuickFilterFn };
};
