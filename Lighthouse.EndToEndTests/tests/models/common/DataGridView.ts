import { expect, type Locator, type Page } from "@playwright/test";

type Box = { x: number; y: number; width: number; height: number };

/** True when the whole page can be scrolled sideways, which a table that stays in its box never causes. */
export async function pageScrollsSideways(page: Page): Promise<boolean> {
	return page.evaluate(
		() =>
			(document.scrollingElement?.scrollWidth ?? 0) >
			(document.scrollingElement?.clientWidth ?? 0),
	);
}

/**
 * What a person sees of one table built on the shared data grid: its box is what is in view, whatever
 * scrolls inside it. The page objects of the lists that use that grid ask these questions through it, so
 * the rules about the pinned actions column and the edge shadows are written once.
 */
export class DataGridView {
	/**
	 * @param table the grid itself
	 * @param edgeMarker the element that marks whether columns are hidden and holds the edge shadows
	 * @param name how the table is called in a failure message
	 */
	constructor(
		private readonly page: Page,
		private readonly table: Locator,
		private readonly edgeMarker: Locator,
		private readonly name: string,
	) {}

	private get scroller(): Locator {
		return this.table.locator(".MuiDataGrid-virtualScroller");
	}

	private get actionsHeader(): Locator {
		return this.table.locator('[role="columnheader"][data-field="actions"]');
	}

	/** True when the button is rendered once and lies wholly inside the table's box. */
	async isWhollyInView(button: Locator): Promise<boolean> {
		const tableBox = await this.table.boundingBox();
		const box =
			(await button.count()) === 1 ? await button.boundingBox() : null;
		return (
			tableBox !== null &&
			box !== null &&
			box.x >= tableBox.x - 1 &&
			box.x + box.width <= tableBox.x + tableBox.width + 1
		);
	}

	/** True when the cell shows its text cut off with an ellipsis. */
	async isCutWithEllipsis(cell: Locator): Promise<boolean> {
		return cell.evaluate((root) =>
			[root, ...Array.from(root.querySelectorAll("*"))].some(
				(element) =>
					getComputedStyle(element).textOverflow === "ellipsis" &&
					element.scrollWidth > element.clientWidth,
			),
		);
	}

	/** True when nothing in the cell is wider than the room it has, so the whole content shows. */
	async isShownWhole(cell: Locator): Promise<boolean> {
		return cell.evaluate((root) =>
			[root, ...Array.from(root.querySelectorAll("*"))].every(
				(element) =>
					element.clientWidth === 0 ||
					element.scrollWidth <= element.clientWidth + 1,
			),
		);
	}

	async horizontalScrollPosition(): Promise<number> {
		return this.scroller.evaluate((element) => element.scrollLeft);
	}

	async scrollToTheEnd(): Promise<void> {
		await this.scroller.evaluate((element) => {
			element.scrollLeft = element.scrollWidth;
			element.dispatchEvent(new Event("scroll"));
		});
	}

	/** True when the table is wider than its box, so the table scrolls rather than the page. */
	async tableScrollsSideways(): Promise<boolean> {
		return this.scroller.evaluate(
			(element) => element.scrollWidth > element.clientWidth,
		);
	}

	/** The shadows on the table's edges: decorative elements that let every click through, with the side each sits on. */
	private async cuesOnTheEdges(): Promise<{ side: string; box: Box }[]> {
		await expect(
			this.edgeMarker,
			"the table must mark whether columns are hidden, or a missing marker would read as no shadow",
		).toHaveCount(1);
		const unlabelled = await this.edgeMarker
			// The grid's own scrollbar track is aria-hidden and positioned too; only an overlay of ours counts.
			.locator(
				'[aria-hidden="true"]:not([data-side]):not([class*="MuiDataGrid"])',
			)
			.evaluateAll(
				(elements) =>
					elements.filter((element) => {
						const style = getComputedStyle(element);
						return (
							(style.position === "absolute" || style.position === "fixed") &&
							(element.textContent ?? "").trim() === ""
						);
					}).length,
			);
		expect(
			unlabelled,
			"a decorative overlay inside the table must name its side with data-side, or an unlabelled shadow could hide behind the cue reading",
		).toBe(0);
		const found: { side: string; box: Box }[] = [];
		const cues = this.edgeMarker.locator("[data-side]");
		expect(
			await cues.count(),
			"a table draws at most one shadow per edge",
		).toBeLessThanOrEqual(2);
		for (const cue of await cues.all()) {
			await expect(
				cue,
				"an edge shadow is decorative and hidden from assistive technology",
			).toHaveAttribute("aria-hidden", "true");
			const letsClicksThrough =
				(await cue.evaluate(
					(element) => getComputedStyle(element).pointerEvents,
				)) === "none";
			const box = await cue.boundingBox();
			if (!letsClicksThrough || box === null || box.width === 0) continue;
			found.push({
				side: (await cue.getAttribute("data-side")) as string,
				box,
			});
		}
		return found;
	}

	/** The sides of the table that carry a shadow. */
	async sidesWithACue(): Promise<string[]> {
		return (await this.cuesOnTheEdges())
			.map((cue) => cue.side)
			.sort((a, b) => a.localeCompare(b));
	}

	/** Pixels between the right shadow's end and the actions column's start; infinite while there is no right shadow. */
	async rightCueDistanceFromActions(): Promise<number> {
		const right = (await this.cuesOnTheEdges()).find(
			(cue) => cue.side === "right",
		);
		if (right === undefined) return Number.POSITIVE_INFINITY;
		return Math.abs(
			right.box.x + right.box.width - (await this.actionsBox()).x,
		);
	}

	private async actionsBox(): Promise<Box> {
		await expect(
			this.actionsHeader,
			`the ${this.name} must have an actions column`,
		).toHaveCount(1);
		return (
			(await this.actionsHeader.boundingBox()) ?? {
				x: Number.NaN,
				y: Number.NaN,
				width: Number.NaN,
				height: Number.NaN,
			}
		);
	}

	async focusedField(): Promise<string> {
		return this.page.evaluate(
			() =>
				document.activeElement
					?.closest('[role="gridcell"]')
					?.getAttribute("data-field") ?? "unknown",
		);
	}

	async focusedCellIsClearOfActions(): Promise<boolean> {
		const cell = this.table.locator('[role="gridcell"]:focus-within');
		const box = (await cell.count()) === 1 ? await cell.boundingBox() : null;
		const tableBox = await this.table.boundingBox();
		const actions = await this.actionsBox();
		if (box === null || tableBox === null || Number.isNaN(actions.x)) {
			return false;
		}
		const isActions = (await this.focusedField()) === "actions";
		const end = isActions ? tableBox.x + tableBox.width : actions.x;
		const endsClear = box.x + box.width <= end + 1;
		// A cell wider than the room before the pinned column cannot also start in view; it must end clear of it.
		const fitsInTheRoom = box.width <= end - tableBox.x;
		return endsClear && (!fitsInTheRoom || box.x >= tableBox.x - 1);
	}

	/**
	 * Moves on from the focused cell with the arrow keys until the actions cell has the focus. Every cell
	 * on the way must come to rest clear of the pinned actions column.
	 */
	async walkToActionsWithTheKeyboard(mostCells: number): Promise<string[]> {
		const visited: string[] = [];
		for (let step = 0; step < mostCells; step++) {
			await expect
				.poll(() => this.focusedField(), {
					message: "a cell of the table must hold the focus",
				})
				.not.toBe("unknown");
			const field = await this.focusedField();
			visited.push(field);
			await expect
				.poll(() => this.focusedCellIsClearOfActions(), {
					message: `the focused "${field}" cell must come to rest clear of the pinned actions column, not under it`,
				})
				.toBe(true);
			if (field === "actions") break;
			await this.page.keyboard.press("ArrowRight");
		}
		return visited;
	}
}
