import type { Locator, Page } from "@playwright/test";

/** Settings > Organization > Terminology. */
export class TerminologyPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get terminologyConfiguration(): Locator {
		return this.page.getByText("Terminology ConfigurationUse");
	}
}
