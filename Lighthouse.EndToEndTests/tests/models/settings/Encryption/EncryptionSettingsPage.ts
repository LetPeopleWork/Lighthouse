import type { Locator, Page } from "@playwright/test";

export class EncryptionSettingsPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get keyRing(): Locator {
		return this.page.getByTestId("encryption-key-ring");
	}

	get custody(): Locator {
		return this.page.getByTestId("encryption-custody");
	}
}
