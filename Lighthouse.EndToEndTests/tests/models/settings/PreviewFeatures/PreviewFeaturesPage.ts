import type { Page } from "@playwright/test";

export class PreviewFeaturesPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}
}
