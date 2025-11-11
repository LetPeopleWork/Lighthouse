import type { Page } from "@playwright/test";

export class DemoDataPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}
}
