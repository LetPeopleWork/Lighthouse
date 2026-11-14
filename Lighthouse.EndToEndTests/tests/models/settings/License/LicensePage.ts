import type { Locator, Page } from "@playwright/test";

/** System, License: the details of the loaded license and the buttons to load, update and clear it. */
export class LicensePage {
	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get panel(): Locator {
		return this.page.getByTestId("license-panel");
	}
}
