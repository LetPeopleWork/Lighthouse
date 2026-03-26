import type { Locator, Page } from "@playwright/test";

export class MisconfiguredPage {
	readonly page: Page;
	readonly container: Locator;
	readonly message: Locator;

	constructor(page: Page) {
		this.page = page;
		this.container = page.getByRole("heading", {
			name: "Authentication Misconfigured",
		});
		this.message = page.getByTestId("misconfigured-message");
	}
}
