import { fireEvent, screen } from "@testing-library/react";
import { expect, vi } from "vitest";

/**
 * Steps shared by every table that sits on the shared grid, so each table states the same promises
 * about its fit once: long text is cut and keeps its full text in a tooltip, numbers, dates and
 * states are shown whole, the actions column is pinned as the last column, and the table says when
 * columns are out of sight.
 */

const PINNED_CLASS = "lighthouse-pinned-actions";

/** The box the grid sits in. jsdom has no layout, so a test states the box the person would see. */
export const box = { content: 0, viewport: 0, scrollLeft: 0 };

/**
 * The grid module with its box stated by `box`. A test file installs it with
 * `vi.mock("@mui/x-data-grid", () => dataGridWithItsBoxStated())`.
 */
export const dataGridWithItsBoxStated = async () => {
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
			return ref;
		},
	};
};

/** The window is narrow: the columns want 900 px and the table has 400, scrolled to its left edge. */
export const givenANarrowWindow = () => {
	box.content = 900;
	box.viewport = 400;
	box.scrollLeft = 0;
};

export const resetTheWindow = () => {
	box.content = 0;
	box.viewport = 0;
	box.scrollLeft = 0;
};

const statedCut: { mockRestore: () => void }[] = [];

/** The browser cuts text wider than its column; the test states the cut. */
export const givenTextIsWiderThanItsColumn = () => {
	statedCut.push(
		vi.spyOn(HTMLElement.prototype, "scrollWidth", "get").mockReturnValue(600),
		vi.spyOn(HTMLElement.prototype, "clientWidth", "get").mockReturnValue(160),
	);
};

/** Back to the layout jsdom has, which is none. */
export const resetTheCut = () => {
	for (const spy of statedCut.splice(0)) spy.mockRestore();
};

const cellOf = (root: ParentNode, field: string): HTMLElement => {
	const cell = root.querySelector<HTMLElement>(
		`[role="gridcell"][data-field="${field}"]`,
	);
	expect(cell, `the table must show a ${field} cell`).not.toBeNull();
	return cell as HTMLElement;
};

const headerOf = (root: ParentNode, field: string): HTMLElement => {
	const header = root.querySelector<HTMLElement>(
		`[role="columnheader"][data-field="${field}"]`,
	);
	expect(header, `the table must show a ${field} column`).not.toBeNull();
	return header as HTMLElement;
};

/** What the cell draws inside itself ends with an ellipsis; a chip's own label is not the column's cut. */
const isCutWithEllipsis = (cell: HTMLElement): boolean =>
	Array.from(cell.querySelectorAll<HTMLElement>("*"))
		.filter((element) => element.closest(".MuiChip-root") === null)
		.some((element) => getComputedStyle(element).textOverflow === "ellipsis");

export const thenTheTableShowsOneGrid = (root: ParentNode) => {
	expect(
		root.querySelectorAll('[role="grid"]').length,
		"the table must be one grid",
	).toBe(1);
};

export const thenALongTextIsCutToItsColumn = (
	root: ParentNode,
	field: string,
) => {
	expect(
		isCutWithEllipsis(cellOf(root, field)),
		`a long ${field} text must end with an ellipsis instead of wrapping the row or being lost; mark the column as text so it shrinks and cuts`,
	).toBe(true);
};

export const thenTheColumnIsShownWhole = (root: ParentNode, field: string) => {
	expect(
		isCutWithEllipsis(cellOf(root, field)),
		`the ${field} cell must show its whole content; numbers, dates and states are never cut`,
	).toBe(false);
};

export const thenTheActionsColumnIsPinnedAndLast = (
	root: ParentNode,
	field: string,
) => {
	const headers = Array.from(
		root.querySelectorAll<HTMLElement>('[role="columnheader"]'),
	).map((header) => header.dataset.field);
	expect(
		headers.at(-1),
		`the columns are ${JSON.stringify(headers)}; the actions column is pinned to the right edge, so it must be the last one`,
	).toBe(field);
	expect(
		headerOf(root, field).classList.contains(PINNED_CLASS),
		`the ${field} header must be pinned, or Delete and Edit scroll out of view`,
	).toBe(true);
	expect(
		cellOf(root, field).classList.contains(PINNED_CLASS),
		`the ${field} cells must be pinned, or Delete and Edit scroll out of view`,
	).toBe(true);
};

const descriptionOf = (grid: HTMLElement): string =>
	(grid.getAttribute("aria-describedby") ?? "")
		.split(" ")
		.map((id) => document.getElementById(id)?.textContent ?? "")
		.join(" ")
		.trim();

export const thenTheTableSaysColumnsAreOutOfSightOnItsRight = (
	root: ParentNode,
) => {
	expect(
		root.querySelector("[data-overflow]")?.getAttribute("data-overflow"),
		"the table must mark that columns are hidden on its right while it is scrolled to its left edge",
	).toBe("right");
	expect(
		root.querySelector('[aria-hidden="true"][data-side="right"]'),
		"a shadow must sit on the right edge so the person sees there is more to scroll to",
	).not.toBeNull();
	expect(
		descriptionOf(root.querySelector<HTMLElement>('[role="grid"]') as HTMLElement),
		"a screen reader user must hear which side has hidden columns",
	).toBe("This table scrolls sideways. More columns to the right.");
};

export const whenThePersonHoversOver = (element: HTMLElement) => {
	fireEvent.mouseOver(element);
};

export const whenThePersonReachesWithTheKeyboard = (element: HTMLElement) => {
	fireEvent.focusIn(element);
};

export const thenTheFullTextIsInATooltip = async (fullText: string) => {
	expect(
		await screen.findByRole("tooltip"),
		"a cut text must show its full text in a tooltip, for a mouse user and for a keyboard user",
	).toHaveTextContent(fullText);
};
