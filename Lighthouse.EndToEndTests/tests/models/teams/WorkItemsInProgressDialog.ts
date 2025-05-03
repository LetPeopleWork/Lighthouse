import type { Page } from "@playwright/test";

export class WorkItemsInProgressDialog {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async close(): Promise<void> {
		const closeButton = this.page.getByRole("button");
		await closeButton.click();
	}
}
