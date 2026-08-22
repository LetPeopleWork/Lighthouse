import type { Page } from "@playwright/test";

export class ArchiveConfirmationDialog {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async cancel(): Promise<void> {
		await this.page.getByRole("button", { name: "Cancel" }).click();
	}

	async archive(): Promise<void> {
		await this.page.getByRole("button", { name: "Archive", exact: true }).click();
	}

	/**
	 * Ticking this writes the choice to this browser, so every later archive skips the dialog
	 * entirely. A test that ticks it changes what the rest of the run sees.
	 */
	async stopAsking(): Promise<void> {
		await this.page.getByTestId("skip-archive-confirmation").click();
	}
}
