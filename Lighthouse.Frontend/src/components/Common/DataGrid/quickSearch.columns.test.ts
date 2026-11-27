import type { GridColDef } from "@mui/x-data-grid";
import { describe, expect, it } from "vitest";
import { shownTextQuickFilter, withShownTextQuickFilter } from "./quickSearch";

type QuickFilterFactory = NonNullable<GridColDef["getApplyQuickFilterFn"]>;

const shownTextOfRow = (shown: string) =>
	({ current: { getRowFormattedValue: () => shown } }) as never;

const testAgainst = (column: GridColDef, typed: string, shown: string) => {
	const apply = (column.getApplyQuickFilterFn as QuickFilterFactory)(
		typed,
		column as never,
		shownTextOfRow(shown),
	);
	return apply === null
		? null
		: apply(undefined, {}, column, shownTextOfRow(shown));
};

describe("a column's search by the text it shows", () => {
	it("keeps a column that brings its own search as it is", () => {
		const own: GridColDef = {
			field: "own",
			getApplyQuickFilterFn: () => null,
		};

		expect(withShownTextQuickFilter(own)).toBe(own);
	});

	it.each(["boolean", "actions", "custom"] as const)(
		"leaves a %s column as it is",
		(type) => {
			const column = { field: "kind", type } as GridColDef;

			expect(withShownTextQuickFilter(column)).toBe(column);
		},
	);

	it("gives a plain text column a search that matches the text it shows, in any letter case", () => {
		const plain: GridColDef = { field: "weight" };

		const searched = withShownTextQuickFilter(plain);

		expect(searched).not.toBe(plain);
		expect(testAgainst(searched, "8 PTS", "8 pts")).toBe(true);
		expect(testAgainst(searched, "9 pts", "8 pts")).toBe(false);
	});

	it("gives a plain text column no search while the typed text is blank", () => {
		const searched = withShownTextQuickFilter({ field: "weight" });

		expect(testAgainst(searched, "   ", "8 pts")).toBeNull();
	});

	it("builds no test from blank typed text", () => {
		expect(shownTextQuickFilter("  ")).toBeNull();
	});
});
