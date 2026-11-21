import { expect, type Locator, type Page } from "@playwright/test";
import { formatLocalDate } from "../../helpers/dates";
import { ReportPage } from "./ReportPage";

/** What a Then & Now report is created with; anything left out stays as the wizard opens it. */
export interface ThenAndNowChoices {
	thenEndsOn?: Date;
	thenDays?: number;
	name?: string;
}

type Box = { x: number; y: number; width: number; height: number };

/**
 * The Reports tab of a Team or a Portfolio, and the wizard that creates a report from it. Left as it
 * opens, the wizard creates a Then & Now report whose Then is the 90 days ending today, with Now as long
 * and all four metrics.
 */
export class ReportsListPage {
	constructor(private readonly page: Page) {}

	/** The list marks itself ready once it has read the reports; until then Create Report waits too. */
	get ready(): Locator {
		return this.page.locator(
			'[data-testid="reports-list"][data-widget-status="ready"]',
		);
	}

	async waitUntilReady(): Promise<ReportsListPage> {
		await this.ready.waitFor();
		return this;
	}

	get createReportButton(): Locator {
		return this.page.getByRole("button", { name: "Create Report" });
	}

	get wizard(): Locator {
		return this.page.getByRole("dialog");
	}

	reportLink(name: string): Locator {
		return this.page.getByRole("link", { name });
	}

	/** Opens the wizard on its Type step. */
	async openCreateReport(): Promise<Locator> {
		await this.waitUntilReady();
		await this.createReportButton.click();
		await this.wizard
			.getByText(/Set today's flow against a window that stays as it was\./)
			.waitFor();
		return this.wizard;
	}

	/** From the Type step to the Then step, which names the days Then and Now cover. */
	async goToThenStep(): Promise<Locator> {
		await this.nextStep();
		await this.wizard.getByText(/^Then \d{1,2} \w{3,4}/).waitFor();
		return this.wizard;
	}

	async createTheDefaultReport(): Promise<ReportPage> {
		return this.createReport({});
	}

	async createReport(choices: ThenAndNowChoices): Promise<ReportPage> {
		await this.openCreateReport();
		await this.goToThenStep();

		if (choices.thenEndsOn !== undefined) {
			await this.wizard
				.getByLabel("Then ends on")
				.fill(formatLocalDate(choices.thenEndsOn));
		}
		if (choices.thenDays !== undefined) {
			await this.wizard
				.getByLabel("Then length in days")
				.fill(String(choices.thenDays));
		}

		await this.nextStep();
		await this.wizard.getByLabel("Name", { exact: true }).waitFor();
		if (choices.name !== undefined) {
			await this.wizard.getByLabel("Name", { exact: true }).fill(choices.name);
		}

		await this.wizard
			.getByRole("button", { name: "Create", exact: true })
			.click();
		await this.page.waitForURL(/\/(teams|portfolios)\/\d+\/reports\/\d+$/);
		return new ReportPage(this.page).waitUntilReady();
	}

	private async nextStep(): Promise<void> {
		await this.wizard.getByRole("button", { name: "Next" }).click();
	}

	/** The table of reports as the person sees it: its box is what is in view, whatever scrolls inside it. */
	get table(): Locator {
		return this.page.getByRole("grid");
	}

	private get scroller(): Locator {
		return this.table.locator(".MuiDataGrid-virtualScroller");
	}

	private rowOf(reportName: string): Locator {
		return this.table
			.getByRole("row")
			.filter({ has: this.page.getByRole("link", { name: reportName }) });
	}

	nameCellOf(reportName: string): Locator {
		return this.rowOf(reportName).locator(
			'[role="gridcell"][data-field="name"]',
		);
	}

	createdCellOf(reportName: string): Locator {
		return this.rowOf(reportName).locator(
			'[role="gridcell"][data-field="createdOn"]',
		);
	}

	get nameHeader(): Locator {
		return this.table.locator('[role="columnheader"][data-field="name"]');
	}

	tooltipShowing(text: string): Locator {
		return this.page.getByRole("tooltip", { name: text });
	}

	async nameColumnWidth(): Promise<number> {
		return (await this.nameHeader.boundingBox())?.width ?? 0;
	}

	/** The names of the given reports whose Delete is not wholly inside the table's box, rendered or not. */
	async reportsWithDeleteOutOfView(reportNames: string[]): Promise<string[]> {
		const tableBox = await this.table.boundingBox();
		const outOfView: string[] = [];
		for (const name of reportNames) {
			const deleteButton = this.page.getByRole("button", {
				name: `Delete ${name}`,
				exact: true,
			});
			const box =
				(await deleteButton.count()) === 1
					? await deleteButton.boundingBox()
					: null;
			const inside =
				tableBox !== null &&
				box !== null &&
				box.x >= tableBox.x - 1 &&
				box.x + box.width <= tableBox.x + tableBox.width + 1;
			if (!inside) outOfView.push(name);
		}
		return outOfView;
	}

	async hoverNameOf(reportName: string): Promise<void> {
		await this.page.getByRole("link", { name: reportName }).hover();
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

	/** The shadows on the table's edges: decorative elements that let every click through, with the side each sits on. */
	private async cuesOnTheEdges(): Promise<{ side: string; box: Box }[]> {
		const root = this.page
			.locator("[data-overflow]")
			.filter({ has: this.table });
		await expect(
			root,
			"the table must mark whether columns are hidden, or a missing marker would read as no shadow",
		).toHaveCount(1);
		const cues = root.locator('[aria-hidden="true"]:not(svg)');
		const found: { side: string; box: Box }[] = [];
		for (const cue of await cues.all()) {
			const letsClicksThrough =
				(await cue.evaluate(
					(element) => getComputedStyle(element).pointerEvents,
				)) === "none";
			const box = await cue.boundingBox();
			if (!letsClicksThrough || box === null || box.width === 0) continue;
			found.push({
				side: (await cue.getAttribute("data-side")) ?? "unknown",
				box,
			});
		}
		return found;
	}

	/** The sides of the table that carry a shadow. */
	async sidesWithACue(): Promise<string[]> {
		const cues = await this.cuesOnTheEdges();
		return cues.map((cue) => cue.side).sort((a, b) => a.localeCompare(b));
	}

	/** How far the right shadow reaches from the table's left edge, or null while there is none. */
	async rightCueEnd(): Promise<number | null> {
		const right = (await this.cuesOnTheEdges()).find(
			(cue) => cue.side === "right",
		);
		return right ? right.box.x + right.box.width : null;
	}

	/** Pixels between the right shadow's end and the actions column's start; infinite while there is no right shadow. */
	async rightCueDistanceFromActions(): Promise<number> {
		const end = await this.rightCueEnd();
		if (end === null) return Number.POSITIVE_INFINITY;
		return Math.abs(end - (await this.actionsColumnStart()));
	}

	/** Where the pinned actions column starts, measured from the same edge as the shadow. */
	async actionsColumnStart(): Promise<number> {
		const header = this.table.locator(
			'[role="columnheader"][data-field="actions"]',
		);
		await expect(
			header,
			"the report list must have an actions column",
		).toHaveCount(1);
		return (await header.boundingBox())?.x ?? Number.NaN;
	}

	/**
	 * Moves through the cells of a report with the arrow keys, starting on its Created date, until the
	 * actions cell has the focus. Every cell on the way must come to rest clear of the pinned actions column.
	 */
	async walkToActionsWithTheKeyboard(reportName: string): Promise<string[]> {
		await this.createdCellOf(reportName).click();
		const visited: string[] = [];
		const MOST_CELLS = 12;
		for (let step = 0; step < MOST_CELLS; step++) {
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

	private async focusedField(): Promise<string> {
		return this.page.evaluate(
			() =>
				document.activeElement
					?.closest('[role="gridcell"]')
					?.getAttribute("data-field") ?? "unknown",
		);
	}

	private async focusedCellIsClearOfActions(): Promise<boolean> {
		const cell = this.table.locator('[role="gridcell"]:focus-within');
		const box = (await cell.count()) === 1 ? await cell.boundingBox() : null;
		const tableBox = await this.table.boundingBox();
		if (box === null || tableBox === null) return false;
		const isActions = (await this.focusedField()) === "actions";
		const end = isActions
			? tableBox.x + tableBox.width
			: await this.actionsColumnStart();
		const endsClear = box.x + box.width <= end + 1;
		// A cell wider than the room before the pinned column cannot also start in view; it must end clear of it.
		const fitsInTheRoom = box.width <= end - tableBox.x;
		return endsClear && (!fitsInTheRoom || box.x >= tableBox.x - 1);
	}

	/** True when the table is wider than its box, so the table scrolls rather than the page. */
	async tableScrollsSideways(): Promise<boolean> {
		return this.scroller.evaluate(
			(element) => element.scrollWidth > element.clientWidth,
		);
	}

	/** True when the whole page can be scrolled sideways, which a table that stays in its box never causes. */
	async pageScrollsSideways(): Promise<boolean> {
		return this.page.evaluate(
			() =>
				(document.scrollingElement?.scrollWidth ?? 0) >
				(document.scrollingElement?.clientWidth ?? 0),
		);
	}

	async scrollTableToTheEnd(): Promise<void> {
		await this.scroller.evaluate((element) => {
			element.scrollLeft = element.scrollWidth;
			element.dispatchEvent(new Event("scroll"));
		});
	}

	/** As if the person had resized the Name column on an earlier visit: the width is stored for the Team's Reports tab. */
	async reopenWithStoredNameWidth(width: number): Promise<ReportsListPage> {
		await this.page.evaluate((nameWidth) => {
			localStorage.setItem(
				"lighthouse:datagrid:reports-team:state",
				JSON.stringify({ columnWidths: { name: nameWidth } }),
			);
		}, width);
		await this.page.reload();
		return this.waitUntilReady();
	}

	async resetLayout(): Promise<void> {
		await this.page.getByTestId("reset-layout-button").click();
	}

	async viewAt(width: number, height: number): Promise<void> {
		await this.page.setViewportSize({ width, height });
	}
}
