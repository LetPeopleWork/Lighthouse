import type { Locator, Page } from "@playwright/test";

export class WorkItemsDialog {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async close(): Promise<void> {
		const closeButton = this.page.getByRole("button").first();
		await closeButton.click();
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
}
