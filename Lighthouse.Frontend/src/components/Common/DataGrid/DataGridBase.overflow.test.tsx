import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import DataGridBase from "./DataGridBase";
import type { DataGridColumn } from "./types";

// The grid measures its own box with layout, which jsdom has none of. The tests state the box the
// person would see, and the grid is left to react to it as it does in a browser.
type GridApi = {
	publishEvent: (name: string, params: unknown) => void;
};

const box = vi.hoisted(() => ({
	content: 0,
	viewport: 0,
	scrollLeft: 0,
	api: undefined as { current: GridApi | undefined } | undefined,
}));

vi.mock("@mui/x-data-grid", async () => {
	const actual =
		await vi.importActual<typeof import("@mui/x-data-grid")>(
			"@mui/x-data-grid",
		);
	const stated = new WeakSet<object>();
	const statedApis = new WeakMap<object, object>();

	const stateTheBox = (api: object) => {
		const known = statedApis.get(api);
		if (known) return known;
		const real = api as { getRootDimensions?: () => object };
		const proxy = new Proxy(api, {
			get(target, property) {
				if (property === "getRootDimensions") {
					return () => ({
						...real.getRootDimensions?.(),
						contentSize: { width: box.content, height: 0 },
						viewportInnerSize: { width: box.viewport, height: 0 },
					});
				}
				if (property === "getScrollPosition") {
					return () => ({ left: box.scrollLeft, top: 0 });
				}
				return Reflect.get(target, property);
			},
		});
		statedApis.set(api, proxy);
		return proxy;
	};

	return {
		...actual,
		useGridApiRef: () => {
			const ref = actual.useGridApiRef();
			if (!stated.has(ref)) {
				stated.add(ref);
				let current = ref.current as object | undefined;
				Object.defineProperty(ref, "current", {
					configurable: true,
					get: () => (current ? stateTheBox(current) : current),
					set: (next: object | undefined) => {
						current = next;
					},
				});
			}
			box.api = ref as unknown as typeof box.api;
			return ref;
		},
	};
});

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

interface ReportRow {
	id: number;
	name: string;
	owner: string;
}

const reportColumns: DataGridColumn<ReportRow>[] = [
	{
		field: "name",
		headerName: "Name",
		flex: 1,
		fit: { kind: "text", minWidth: 160 },
	},
	{ field: "owner", headerName: "Owner", flex: 1, fit: { kind: "text" } },
	{
		field: "actions",
		headerName: "Actions",
		width: 100,
		sortable: false,
		fit: { kind: "actions" },
		renderCell: ({ row }) => (
			<button type="button">{`Delete ${row.name}`}</button>
		),
	},
];

const reportRows: ReportRow[] = [
	{ id: 1, name: "Since August", owner: "Team Lightspeed" },
];

type Edges = "none" | "left" | "right" | "both";

const DESCRIPTION_OF: Record<Exclude<Edges, "none">, string> = {
	right: "This table scrolls sideways. More columns to the right.",
	left: "This table scrolls sideways. More columns to the left.",
	both: "This table scrolls sideways. More columns on both sides.",
};

const CUE_SIDES: Record<Edges, string[]> = {
	none: [],
	left: ["left"],
	right: ["right"],
	both: ["left", "right"],
};

beforeEach(() => {
	localStorage.clear();
	box.content = 900;
	box.viewport = 400;
	box.scrollLeft = 0;
});

afterEach(() => {
	vi.restoreAllMocks();
});

const whenTheReportListIsShown = () =>
	render(
		<DataGridBase
			rows={reportRows}
			columns={reportColumns}
			storageKey="overflow-reports"
		/>,
	);

const whenThePersonScrollsTo = (left: number) => {
	box.scrollLeft = left;
	act(() => {
		box.api?.current?.publishEvent("scrollPositionChange", { left, top: 0 });
	});
};

const whenTheViewportBecomes = (width: number) => {
	box.viewport = width;
	act(() => {
		box.api?.current?.publishEvent("resize", { width, height: 300 });
	});
};

const theCueState = (container: HTMLElement) =>
	container.querySelector<HTMLElement>("[data-overflow]")?.dataset.overflow;

/** The sides of the decorative edge shadows: hidden from assistive technology and never catching a click. */
const theCueSides = (container: HTMLElement) =>
	Array.from(container.querySelectorAll<HTMLElement>('[aria-hidden="true"]'))
		.filter(
			(element) =>
				!(element instanceof SVGElement) &&
				getComputedStyle(element).pointerEvents === "none",
		)
		.map((element) => element.dataset.side ?? "unmarked")
		.sort((a, b) => a.localeCompare(b));

const theGridDescription = () => {
	const ids = screen.getByRole("grid").getAttribute("aria-describedby");
	if (ids === null) return null;
	return ids
		.split(" ")
		.map((id) => document.getElementById(id)?.textContent ?? "")
		.join(" ")
		.trim();
};

const thenTheCueShows = (container: HTMLElement, edges: Edges) => {
	expect(
		theCueState(container),
		`with the table state "${edges}", the grid must mark that state, so the cue follows the scroll position and goes when nothing is hidden`,
	).toBe(edges);
	expect(
		theCueSides(container),
		`the table state "${edges}" must draw a decorative edge shadow on exactly the sides ${JSON.stringify(CUE_SIDES[edges])}, each marked with data-side naming its edge: a shadow on the end of the range, on a table that fits or on the wrong edge tells the person of columns that are not there`,
	).toEqual(CUE_SIDES[edges]);
	if (edges === "none") {
		expect(
			theGridDescription(),
			"a table that fits must not tell a screen reader that it scrolls sideways",
		).toBeNull();
	} else {
		expect(
			theGridDescription(),
			"a screen reader user must hear which side has hidden columns; a shadow alone tells them nothing",
		).toBe(DESCRIPTION_OF[edges]);
	}
};

describe("A table that has more columns than shown says so", () => {
	it("shows a cue on the side with hidden columns while the person scrolls, and none once the table fits", () => {
		const { container } = whenTheReportListIsShown();

		whenThePersonScrollsTo(0);
		thenTheCueShows(container, "right");

		whenThePersonScrollsTo(250);
		thenTheCueShows(container, "both");

		whenThePersonScrollsTo(500);
		thenTheCueShows(container, "left");

		whenTheViewportBecomes(1000);
		thenTheCueShows(container, "none");
	});
});

describe("A person using the keyboard reaches columns that are off screen", () => {
	const PINNED_LEFT = 300;
	const CELL_LEFT_IN_VIEW = 0;

	/** Column places as the person sees them while the table is scrolled: the actions column stays where it is. */
	const placeColumnsLikeTheBrowser = (container: HTMLElement) => {
		const scroller = container.querySelector(".MuiDataGrid-virtualScroller");
		const placeOf = (element: Element) => {
			const field = (element as HTMLElement).dataset.field;
			const scrolled = scroller?.scrollLeft ?? 0;
			if (element === scroller) return { left: 0, right: 400 };
			if (field === "actions") return { left: PINNED_LEFT, right: 400 };
			if (field === "name")
				return { left: 20 - scrolled, right: 140 - scrolled };
			if (field === "owner")
				return { left: 200 - scrolled, right: 320 - scrolled };
			return { left: 0, right: 0 };
		};
		vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockImplementation(
			function (this: HTMLElement) {
				const { left, right } = placeOf(this);
				return {
					left,
					right,
					x: left,
					width: right - left,
					top: 0,
					bottom: 40,
					y: 0,
					height: 40,
					toJSON: () => ({}),
				};
			},
		);
		return scroller as HTMLElement;
	};

	const cellOf = (field: string) =>
		document.querySelector(`[role="gridcell"][data-field="${field}"]`);

	const whenTheyLandOnTheCell = async (
		user: ReturnType<typeof userEvent.setup>,
		field: string,
	) => {
		await user.click(cellOf(field) as Element);
		expect(
			document.activeElement,
			`the ${field} cell must take the focus`,
		).toBe(cellOf(field));
	};

	const whenTheyArrowToTheCell = async (
		user: ReturnType<typeof userEvent.setup>,
		field: string,
	) => {
		await user.keyboard("{ArrowRight}");
		expect(
			document.activeElement,
			`the arrow key must move the focus to the ${field} cell`,
		).toBe(cellOf(field));
		return cellOf(field) as HTMLElement;
	};

	it("scrolls a focused cell clear of the pinned actions column, leaves a cell that is in view where it is, and lets the keyboard reach the actions column", async () => {
		const user = userEvent.setup();
		const { container } = whenTheReportListIsShown();
		const scroller = placeColumnsLikeTheBrowser(container);
		whenThePersonScrollsTo(0);

		await whenTheyLandOnTheCell(user, "name");
		expect(
			scroller.scrollLeft,
			"a focused cell that is wholly in view must not make the table scroll",
		).toBe(0);

		const owner = await whenTheyArrowToTheCell(user, "owner");
		const place = owner.getBoundingClientRect();
		expect(
			place.right,
			"the focused Owner cell must end before the pinned actions column; it was left under it, out of the keyboard user's sight",
		).toBeLessThanOrEqual(PINNED_LEFT);
		expect(
			place.left,
			"scrolling the focused cell clear of the pinned column must not push its start out of view",
		).toBeGreaterThanOrEqual(CELL_LEFT_IN_VIEW);

		await whenTheyArrowToTheCell(user, "actions");
	});

	it("leaves the table where it is when the actions cell itself takes the focus", async () => {
		const user = userEvent.setup();
		const { container } = whenTheReportListIsShown();
		const scroller = placeColumnsLikeTheBrowser(container);
		whenThePersonScrollsTo(0);

		await whenTheyLandOnTheCell(user, "actions");
		await act(async () => {
			await new Promise((resolve) => setTimeout(resolve, 5));
		});
		expect(
			scroller.scrollLeft,
			"the pinned cell never sits under itself, so focusing it must not scroll",
		).toBe(0);
	});

	it("scrolls a focused cell back into view when it starts before the table's left edge", async () => {
		const user = userEvent.setup();
		const { container } = whenTheReportListIsShown();
		const scroller = placeColumnsLikeTheBrowser(container);
		whenThePersonScrollsTo(0);
		scroller.scrollLeft = 70;

		await user.click(cellOf("name") as Element);
		await act(async () => {
			await new Promise((resolve) => setTimeout(resolve, 5));
		});
		expect(
			scroller.scrollLeft,
			"the Name cell started 50 px before the left edge, so the table scrolls back by 50 px",
		).toBe(20);
	});
});

const styleOf = (element: Element | null) =>
	getComputedStyle(element as Element);

const theShadow = (container: HTMLElement, side: "left" | "right") =>
	container.querySelector<HTMLElement>(`[data-side="${side}"]`) as HTMLElement;

describe("The edge shadows sit on the edge they name", () => {
	it("draws the left shadow at the left edge fading to the right, and the right shadow ending where the pinned actions column starts, fading to the left", () => {
		box.scrollLeft = 250;
		const { container } = whenTheReportListIsShown();
		whenThePersonScrollsTo(250);

		const left = styleOf(theShadow(container, "left"));
		const right = styleOf(theShadow(container, "right"));
		expect(left.position, "a shadow is laid over the table").toBe("absolute");
		expect(left.left, "the left shadow starts at the left edge").toBe("0px");
		expect(
			left.background,
			"the left shadow is darkest at its own edge",
		).toContain("to right");
		expect(
			right.right,
			"the right shadow must end where the 100 px pinned actions column starts, not under it",
		).toBe("100px");
		expect(
			right.background,
			"the right shadow is darkest at its own edge",
		).toContain("to left");
	});

	it("lets the right shadow reach the edge when the pinned actions column is hidden", () => {
		localStorage.setItem(
			"lighthouse:datagrid:overflow-reports:state",
			JSON.stringify({ columnVisibilityModel: { actions: false } }),
		);
		const { container } = whenTheReportListIsShown();
		whenThePersonScrollsTo(0);

		expect(
			styleOf(theShadow(container, "right")).right,
			"with no pinned column there is nothing for the shadow to stop before",
		).toBe("0px");
	});

	it("puts nothing over a table that fits", () => {
		box.viewport = 1000;
		const { container } = whenTheReportListIsShown();
		whenThePersonScrollsTo(0);

		const grid = container.querySelector(".MuiDataGrid-root") as HTMLElement;
		expect(
			(grid.parentElement as HTMLElement).firstElementChild,
			"a table that fits adds no hidden sentence and no shadow beside the grid",
		).toBe(grid);
	});
});

describe("The table keeps its own box and its row look", () => {
	it("holds the shadows in a box of its own, lets rows grow with their text, and styles cells and the pinned column", () => {
		const { container } = whenTheReportListIsShown();
		const grid = container.querySelector(".MuiDataGrid-root") as HTMLElement;

		expect(
			styleOf(grid.parentElement).position,
			"the shadows are placed against this box, so it must be the positioned one",
		).toBe("relative");
		expect(
			container.querySelector(".MuiDataGrid-row--dynamicHeight"),
			"a row grows with its content instead of cutting it off",
		).not.toBeNull();

		const cell = document.querySelector(
			'[role="gridcell"][data-field="name"]',
		) as HTMLElement;
		const cellStyle = styleOf(cell);
		expect(cellStyle.display).toBe("flex");
		expect(cellStyle.alignItems).toBe("center");
		expect(cellStyle.whiteSpace).toBe("normal");
		expect(cellStyle.wordWrap).toBe("break-word");
		expect(
			styleOf(cell.closest(".MuiDataGrid-row")).maxHeight,
			"the row height is left to its content",
		).toBe("none");

		const pinned = styleOf(
			document.querySelector('[role="gridcell"][data-field="actions"]'),
		);
		expect(pinned.position).toBe("sticky");
		expect(pinned.right).toBe("0px");
		expect(pinned.zIndex).toBe("3");
	});

	it("sorts by the sort model it starts with, and remembers a sort the person picks", async () => {
		const user = userEvent.setup();
		const onSort = vi.fn();
		render(
			<DataGridBase
				rows={[
					{ id: 1, name: "Alpha", owner: "x" },
					{ id: 2, name: "Beta", owner: "y" },
				]}
				columns={reportColumns}
				storageKey="overflow-sorting"
				initialSortModel={[{ field: "name", sort: "desc" }]}
				onSortModelChange={onSort}
			/>,
		);

		const firstRow = () =>
			document.querySelector('[role="row"][data-id]')?.getAttribute("data-id");
		expect(firstRow(), "descending by name starts with Beta").toBe("2");

		await user.click(screen.getByRole("columnheader", { name: /Owner/ }));
		const stored = JSON.parse(
			localStorage.getItem("lighthouse:datagrid:overflow-sorting:state") ??
				"{}",
		);
		expect(
			stored.sortModel,
			"the sort the person picked is remembered",
		).toEqual([{ field: "owner", sort: "asc" }]);
		expect(onSort).toHaveBeenCalledWith([{ field: "owner", sort: "asc" }]);
	});
});

describe("Resizing a column is remembered once the person stops", () => {
	const storedWidths = () =>
		JSON.parse(
			localStorage.getItem("lighthouse:datagrid:overflow-reports:state") ??
				"{}",
		).columnWidths;
	const whenTheyResize = (field: string | undefined, width: number) =>
		act(() => {
			box.api?.current?.publishEvent("columnResize", {
				width,
				colDef: field === undefined ? undefined : { field },
			});
		});

	it("writes only the resized columns, 250 ms after the last drag", () => {
		vi.useFakeTimers();
		try {
			whenTheReportListIsShown();
			whenTheyResize("name", 150);
			vi.advanceTimersByTime(200);
			whenTheyResize("owner", 180);
			vi.advanceTimersByTime(100);
			expect(
				storedWidths(),
				"a drag that goes on must not be written half way",
			).toBeUndefined();

			vi.advanceTimersByTime(150);
			expect(storedWidths()).toEqual({ name: 150, owner: 180 });
		} finally {
			vi.useRealTimers();
		}
	});

	it("applies the dragged width to the column at once", () => {
		const { container } = whenTheReportListIsShown();
		whenTheyResize("owner", 333);

		const header = container.querySelector(
			'[role="columnheader"][data-field="owner"]',
		) as HTMLElement;
		expect(header.style.width).toBe("333px");
	});

	it("ignores a drag that names no column, and still writes nothing wrong", () => {
		vi.useFakeTimers();
		const warn = vi.spyOn(console, "warn").mockImplementation(() => {});
		try {
			whenTheReportListIsShown();
			whenTheyResize(undefined, 150);
			vi.advanceTimersByTime(300);
			expect(
				warn,
				"a drag without a column is skipped quietly",
			).not.toHaveBeenCalled();
			expect(storedWidths()).toEqual({});
		} finally {
			vi.useRealTimers();
		}
	});

	it("says so, and keeps going, when a drag cannot be read", () => {
		vi.useFakeTimers();
		const warn = vi.spyOn(console, "warn").mockImplementation(() => {});
		try {
			whenTheReportListIsShown();
			act(() => {
				box.api?.current?.publishEvent("columnResize", {
					get colDef(): never {
						throw new Error("unreadable");
					},
				});
			});
			expect(warn).toHaveBeenCalledWith(
				"Failed to handle column resize",
				expect.any(Error),
			);
		} finally {
			vi.useRealTimers();
		}
	});
});
