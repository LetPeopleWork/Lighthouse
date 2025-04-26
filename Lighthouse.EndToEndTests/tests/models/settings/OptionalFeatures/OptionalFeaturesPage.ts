import type { Page } from "@playwright/test";

export class OptionalFeaturesPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}
}
