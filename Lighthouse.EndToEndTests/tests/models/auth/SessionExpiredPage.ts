import type { Locator, Page } from "@playwright/test";

export class SessionExpiredPage {
	readonly page: Page;
	readonly container: Locator;
	readonly signInAgainButton: Locator;

	constructor(page: Page) {
		this.page = page;
		this.container = page.getByTestId("session-expired-page");
		this.signInAgainButton = page.getByTestId("session-expired-login-button");
	}

	async clickSignInAgain(): Promise<void> {
		await this.signInAgainButton.click();
	}
}
