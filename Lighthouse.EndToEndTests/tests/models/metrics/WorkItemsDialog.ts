import type { Locator, Page } from "@playwright/test";

export class WorkItemsDialog {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async close(): Promise<void> {
		// Scoped to the dialog on purpose: the accessible-name match is a case-insensitive
		// substring, and the feature size scatter plot behind this dialog renders a "Closed Date"
		// toggle that an unscoped lookup would find as well.
		await this.dialog.getByRole("button", { name: "Close" }).click();
	}

	get enlargeButton(): Locator {
		return this.dialog.getByRole("button", { name: "Enlarge" });
	}

	get dialog(): Locator {
		return this.page.getByRole("dialog");
	}

	get title(): Locator {
		// The DialogTitle renders the "{context} Completed" / "{context} Started"
		// heading that identifies which widget's data set is on screen.
		return this.dialog.locator(".MuiDialogTitle-root");
	}

	get rows(): Locator {
		return this.dialog.locator(".MuiDataGrid-row");
	}

	async countRows(): Promise<number> {
		return this.rows.count();
	}

	/**
	 * A column found by what its header says, never by where it stands: each context orders its own
	 * columns, and a reader can reorder them.
	 */
	columnHeader(name: string | RegExp): Locator {
		return this.dialog.getByRole("columnheader", { name });
	}

	/** The cell under the named column on the row whose ID is exactly `reference`. */
	async cellIn(
		columnName: string | RegExp,
		reference: string,
	): Promise<Locator> {
		const columnIndex =
			await this.columnHeader(columnName).getAttribute("aria-colindex");
		const exactly = new RegExp(
			`^${reference.replaceAll(/[.*+?^${}()|[\]\\]/g, "\\$&")}$`,
		);
		return this.rows
			.filter({
				has: this.page.locator('[data-field="referenceId"]', {
					hasText: exactly,
				}),
			})
			.locator(`[role="gridcell"][aria-colindex="${columnIndex}"]`);
	}

	/** Every cell under the named column, one per row on screen. */
	async cellsIn(columnName: string | RegExp): Promise<Locator> {
		const columnIndex =
			await this.columnHeader(columnName).getAttribute("aria-colindex");
		return this.rows.locator(
			`[role="gridcell"][aria-colindex="${columnIndex}"]`,
		);
	}

	/** Opens the grid's own list of every column it can show, from the Name column's menu. */
	async openManageColumns(): Promise<void> {
		await this.dialog
			.getByRole("columnheader", { name: "Name", exact: true })
			.hover();
		await this.dialog
			.getByRole("button", { name: "Name column menu", exact: true })
			.click();
		await this.page
			.getByRole("menuitem", { name: "Manage columns", exact: true })
			.click();
	}

	get timeInStateColumnHeader(): Locator {
		return this.page.getByRole("columnheader", { name: "Time in State" });
	}

	get timeInStateCells(): Locator {
		return this.page.getByRole("gridcell").filter({ hasText: /\bin\b/ });
	}

	async getTimeInStateBadges(): Promise<string[]> {
		const grid = this.page.getByRole("grid");
		const cells = grid.getByRole("gridcell").filter({ hasText: /d in / });
		return cells.allInnerTexts();
	}

	async sortByTimeInState(): Promise<void> {
		await this.timeInStateColumnHeader.click();
	}

	staleTimeInStateBadgeFor(workItemReference: string): Locator {
		return this.page
			.getByRole("row")
			.filter({ hasText: workItemReference })
			.getByTestId("time-in-state-stale");
	}

	async countStaleTimeInStateBadges(): Promise<number> {
		return this.page.getByTestId("time-in-state-stale").count();
	}
}
