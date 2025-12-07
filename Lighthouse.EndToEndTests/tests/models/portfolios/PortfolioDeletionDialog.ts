import type { Page } from "@playwright/test";

export class PortfolioDeletionDialog {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async cancel(): Promise<void> {
		const cancelButton = this.page.getByRole("button", { name: "Cancel" });
		await cancelButton.click();
	}

	async delete(): Promise<void> {
		const deleteButton = this.page.getByRole("button", { name: "Delete" });
		await deleteButton.click();
	}
}
