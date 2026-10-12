import { render, screen, waitFor } from "@testing-library/react";
import userEvent, { type UserEvent } from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import DataGridBase from "./DataGridBase";
import type { DataGridColumn } from "./types";

/**
 * Every list in Lighthouse is a DataGridBase. A list with more than 100 rows says how many it has
 * and pages to the rest in fixed pages of 100; a list of 100 rows or fewer looks exactly as before.
 * These run the real MUI grid, so the page policy pinned here is the one a reader gets.
 */

const { premium } = vi.hoisted(() => ({ premium: { current: false } }));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: premium.current },
		isLoading: false,
	}),
}));

const RENDER_HEAVY = 20000;

interface ListedItem {
	id: number;
	name: string;
	stage: string;
}

const columns: DataGridColumn<ListedItem>[] = [
	{ field: "name", headerName: "Name", width: 150 },
	{ field: "stage", headerName: "Stage", width: 120 },
];

/** In a 243-row list, 64 rows are in Review, 150 Doing and 29 Done, so a Stage filter can leave 64 or 179. */
const stageOf = (position: number): string => {
	if (position < 64) return "Review";
	if (position < 214) return "Doing";
	return "Done";
};

/** Rows named "Row 001", "Row 002", … in that order. */
const aListOf = (count: number): ListedItem[] =>
	Array.from({ length: count }, (_, position) => ({
		id: position + 1,
		name: `Row ${String(position + 1).padStart(3, "0")}`,
		stage: stageOf(position),
	}));

const namesOf = (rows: ListedItem[]) => rows.map((row) => row.name);

const STORAGE_KEY = "paged-list";
const SAVED_LAYOUT_KEY = `lighthouse:datagrid:${STORAGE_KEY}:state`;

const showTheList = (rows: ListedItem[], loading = false) => {
	const user = userEvent.setup();
	const theList = (shownRows: ListedItem[], isLoading: boolean) => (
		<DataGridBase
			rows={shownRows}
			columns={columns}
			storageKey={STORAGE_KEY}
			loading={isLoading}
			enableExport
		/>
	);
	const { rerender } = render(theList(rows, loading));
	const reload = (nextRows: ListedItem[], isLoading = false) =>
		rerender(theList(nextRows, isLoading));
	return { user, reload };
};

/** The names on the rows the grid shows right now, top to bottom. */
const namesShown = () =>
	screen.getAllByRole("row").flatMap((row) => {
		const cell = row.querySelector('[role="gridcell"][data-field="name"]');
		return cell === null ? [] : [cell.textContent ?? ""];
	});

const ROW_RANGE = /^\d+–\d+ of \d+$/;

const nextPage = () => screen.getByRole("button", { name: "Go to next page" });
const previousPage = () =>
	screen.getByRole("button", { name: "Go to previous page" });

const goToNextPage = async (user: UserEvent, rangeThere: string) => {
	await user.click(nextPage());
	await screen.findByText(rangeThere);
};

const goToPreviousPage = async (user: UserEvent, rangeThere: string) => {
	await user.click(previousPage());
	await screen.findByText(rangeThere);
};

/**
 * No footer at all: no range, no arrows, no rows-per-page choice, and no footer bar under the rows.
 * The bar has no role or text of its own, so its class is the only way to see that an empty bar,
 * which would still make a short list taller, is not there.
 */
const expectNoFooter = () => {
	expect(screen.queryByText(ROW_RANGE)).not.toBeInTheDocument();
	expect(
		screen.queryByRole("button", { name: "Go to next page" }),
	).not.toBeInTheDocument();
	expect(
		screen.queryByRole("button", { name: "Go to previous page" }),
	).not.toBeInTheDocument();
	expect(screen.queryByText(/^Rows per page/)).not.toBeInTheDocument();
	expect(document.querySelector(".MuiDataGrid-footerContainer")).toBeNull();
};

const sortByNameDescending = async (user: UserEvent) => {
	const header = screen.getByRole("columnheader", { name: /^Name/ });
	await user.click(header);
	await user.click(header);
	await waitFor(() => expect(namesShown()[0]).toBe("Row 243"));
};

/** Filters the Stage column through its column menu, the way a reader would. */
const filterStageBy = async (user: UserEvent, value: string) => {
	await user.click(screen.getByLabelText("Stage column menu"));
	await user.click(await screen.findByRole("menuitem", { name: "Filter" }));
	await user.type(await screen.findByRole("textbox", { name: "Value" }), value);
};

beforeEach(() => {
	localStorage.clear();
	premium.current = false;
});

describe("A list of 100 rows or fewer looks unchanged", () => {
	// @us-01 @slice-01 @regression @boundary @kpi-OUT-6202-short-lists-untouched @contract-shape:unbounded-preservation
	it.each([0, 1, 100])(
		"shows all of its %i rows and no footer",
		(count) => {
			showTheList(aListOf(count));

			expect(namesShown()).toEqual(namesOf(aListOf(count)));
			expectNoFooter();
		},
		RENDER_HEAVY,
	);
});

describe("A list of more than 100 rows says how many it has and pages to the rest", () => {
	// @us-01 @slice-01 @driving_port @boundary @contract-shape:bounded-change
	it.skip.each([
		[101, "1–100 of 101"],
		[143, "1–100 of 143"],
		[243, "1–100 of 243"],
	])(
		"with %i rows shows the first 100 above %s, with page arrows",
		(count, range) => {
			showTheList(aListOf(count));

			expect(screen.getByText(range)).toBeInTheDocument();
			expect(namesShown()).toEqual(namesOf(aListOf(100)));
			expect(nextPage()).toBeEnabled();
			expect(previousPage()).toBeDisabled();
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	it.skip(
		"puts the 101st row alone on the second page of a 101-row list",
		async () => {
			const { user } = showTheList(aListOf(101));

			await goToNextPage(user, "101–101 of 101");

			expect(namesShown()).toEqual(["Row 101"]);
			expect(nextPage()).toBeDisabled();
			expect(previousPage()).toBeEnabled();
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it.skip(
		"reaches every row of a 243-row list, and the arrows lead back",
		async () => {
			const { user } = showTheList(aListOf(243));

			await goToNextPage(user, "101–200 of 243");
			expect(namesShown()).toEqual(namesOf(aListOf(200).slice(100)));

			await goToNextPage(user, "201–243 of 243");
			expect(namesShown()).toEqual(namesOf(aListOf(243).slice(200)));
			expect(nextPage()).toBeDisabled();

			await goToPreviousPage(user, "101–200 of 243");
			expect(namesShown()[0]).toBe("Row 101");
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it.skip(
		"offers the previous and next page only, with no rows-per-page choice",
		() => {
			showTheList(aListOf(243));

			expect(screen.getByText("1–100 of 243")).toBeInTheDocument();
			expect(screen.queryByText(/^Rows per page/)).not.toBeInTheDocument();
			expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:unbounded-preservation
	// Paging is never remembered: the whole saved entry is the same after paging as before it.
	it.skip(
		"leaves the saved layout of the list exactly as it was after paging",
		async () => {
			const { user } = showTheList(aListOf(243));
			await user.click(screen.getByRole("columnheader", { name: /^Name/ }));
			const savedBefore = localStorage.getItem(SAVED_LAYOUT_KEY);
			expect(savedBefore).not.toBeNull();

			await goToNextPage(user, "101–200 of 243");
			await goToNextPage(user, "201–243 of 243");
			await goToPreviousPage(user, "101–200 of 243");

			expect(localStorage.getItem(SAVED_LAYOUT_KEY)).toBe(savedBefore);
		},
		RENDER_HEAVY,
	);
});

describe("Sorting and filtering a long list", () => {
	// @us-01 @slice-01 @regression @contract-shape:bounded-change
	it(
		"sorts the whole list, so its overall first row opens the first page",
		async () => {
			const { user } = showTheList(aListOf(243));

			await sortByNameDescending(user);

			expect(namesShown()).toEqual(
				namesOf(aListOf(243)).reverse().slice(0, 100),
			);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	it.skip(
		"goes back to the first page when the list is sorted on the second",
		async () => {
			const { user } = showTheList(aListOf(243));
			await goToNextPage(user, "101–200 of 243");

			await sortByNameDescending(user);

			expect(screen.getByText("1–100 of 243")).toBeInTheDocument();
			expect(namesShown()).toEqual(
				namesOf(aListOf(243)).reverse().slice(0, 100),
			);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	it.skip(
		"opens the first page of what a column filter on the second page leaves, when that is still more than 100",
		async () => {
			const { user } = showTheList(aListOf(243));
			await goToNextPage(user, "101–200 of 243");

			await filterStageBy(user, "Do");

			expect(await screen.findByText("1–100 of 179")).toBeInTheDocument();
			expect(namesShown()[0]).toBe("Row 065");
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	// The Work Items dialog has no search of its own, so its column filter is how a list shrinks.
	it.skip(
		"shows all 64 rows a column filter on the second page leaves, with no footer",
		async () => {
			const { user } = showTheList(aListOf(243));
			await goToNextPage(user, "101–200 of 243");

			await filterStageBy(user, "Review");

			await waitFor(() => expect(namesShown()).toEqual(namesOf(aListOf(64))));
			expectNoFooter();
		},
		RENDER_HEAVY,
	);
});

describe("A long list whose rows change while a later page is open", () => {
	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it.skip(
		"shows all 64 rows and no footer when a reload brings the list down to 64",
		async () => {
			const { user, reload } = showTheList(aListOf(243));
			await goToNextPage(user, "101–200 of 243");

			reload(aListOf(64));

			await waitFor(() => expect(namesShown()).toEqual(namesOf(aListOf(64))));
			expectNoFooter();
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	it.skip(
		"keeps the open page when a reload leaves more than 100 rows and the page still exists",
		async () => {
			const { user, reload } = showTheList(aListOf(243));
			await goToNextPage(user, "101–200 of 243");

			reload(aListOf(250));

			expect(await screen.findByText("101–200 of 250")).toBeInTheDocument();
			expect(namesShown()[0]).toBe("Row 101");
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it.skip(
		"lands on the last page left, never an empty one, when a reload removes the open page",
		async () => {
			const { user, reload } = showTheList(aListOf(243));
			await goToNextPage(user, "101–200 of 243");
			await goToNextPage(user, "201–243 of 243");

			reload(aListOf(150));

			expect(await screen.findByText("101–150 of 150")).toBeInTheDocument();
			expect(namesShown()).toEqual(namesOf(aListOf(150).slice(100)));
		},
		RENDER_HEAVY,
	);
});

describe("While a list is loading", () => {
	// @us-01 @slice-01 @regression @contract-shape:unbounded-preservation
	it(
		"shows the loading look and no footer on a first load with no rows yet",
		() => {
			showTheList([], true);

			// With no rows to dim, the grid's own loading look is its skeleton overlay.
			expect(
				document.querySelector(".MuiDataGrid-skeletonLoadingOverlay"),
			).not.toBeNull();
			expectNoFooter();
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	// A range left over from the previous answer under a loading overlay would be a stale count.
	it.skip(
		"shows no footer while a 243-row list reloads, and the range again once it has",
		async () => {
			const { reload } = showTheList(aListOf(243), true);

			expectNoFooter();

			reload(aListOf(243), false);

			expect(await screen.findByText("1–100 of 243")).toBeInTheDocument();
		},
		RENDER_HEAVY,
	);
});

describe("Export and Copy take the whole list, whichever page is open", () => {
	const exported: Blob[] = [];
	const copied: Record<string, Blob>[] = [];
	const { createObjectURL, revokeObjectURL } = URL;
	const clipboard = Object.getOwnPropertyDescriptor(navigator, "clipboard");

	afterEach(() => {
		vi.unstubAllGlobals();
		URL.createObjectURL = createObjectURL;
		URL.revokeObjectURL = revokeObjectURL;
		if (clipboard) {
			Object.defineProperty(navigator, "clipboard", clipboard);
		} else {
			Reflect.deleteProperty(navigator, "clipboard");
		}
	});

	beforeEach(() => {
		premium.current = true;
		exported.length = 0;
		copied.length = 0;
		URL.createObjectURL = vi.fn((blob: Blob | MediaSource) => {
			exported.push(blob as Blob);
			return "blob:paged-list";
		});
		URL.revokeObjectURL = vi.fn();
		vi.stubGlobal(
			"ClipboardItem",
			class {
				constructor(items: Record<string, Blob>) {
					copied.push(items);
				}
			},
		);
		Object.defineProperty(navigator, "clipboard", {
			configurable: true,
			value: { write: vi.fn().mockResolvedValue(undefined) },
		});
	});

	/** The names in the exported file, one per data row, in file order. */
	const namesInTheFile = async () => {
		await waitFor(() => expect(exported).toHaveLength(1));
		const text = (await exported[0].text()).replace("﻿", "");
		return text
			.split("\n")
			.slice(1)
			.map((line) => line.split(",")[0]);
	};

	/** The names in the copied plain text, one per data row, in copied order. */
	const namesCopied = async () => {
		await waitFor(() => expect(copied).toHaveLength(1));
		const text = await copied[0]["text/plain"].text();
		return text
			.split("\n")
			.slice(1)
			.map((line) => line.split("\t")[0]);
	};

	const allNamesDescending = namesOf(aListOf(243)).reverse();

	// @us-01 @slice-01 @regression @contract-shape:unbounded-preservation
	it(
		"exports all 243 rows, in the order shown, from the first page",
		async () => {
			const { user } = showTheList(aListOf(243));
			await sortByNameDescending(user);

			await user.click(screen.getByTestId("export-button"));

			expect(await namesInTheFile()).toEqual(allNamesDescending);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:unbounded-preservation
	it.skip(
		"exports all 243 rows, in the order shown, from the second page",
		async () => {
			const { user } = showTheList(aListOf(243));
			await sortByNameDescending(user);
			await goToNextPage(user, "101–200 of 243");

			await user.click(screen.getByTestId("export-button"));

			expect(await namesInTheFile()).toEqual(allNamesDescending);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @regression @contract-shape:unbounded-preservation
	it(
		"copies all 243 rows, in the order shown, from the first page",
		async () => {
			const { user } = showTheList(aListOf(243));
			await sortByNameDescending(user);

			await user.click(screen.getByTestId("copy-button"));

			expect(await namesCopied()).toEqual(allNamesDescending);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01 @boundary @contract-shape:unbounded-preservation
	it.skip(
		"copies all 243 rows, in the order shown, from the second page",
		async () => {
			const { user } = showTheList(aListOf(243));
			await sortByNameDescending(user);
			await goToNextPage(user, "101–200 of 243");

			await user.click(screen.getByTestId("copy-button"));

			expect(await namesCopied()).toEqual(allNamesDescending);
		},
		RENDER_HEAVY,
	);
});
