import type { Locator, Page } from "@playwright/test";
import { KeycloakLoginPage } from "./KeycloakLoginPage";

export class LoginPage {
	readonly page: Page;
	readonly container: Locator;
	readonly signInButton: Locator;

	constructor(page: Page) {
		this.page = page;
		this.container = page.getByTestId("login-page");
		this.signInButton = page.getByTestId("login-button");
	}

	async clickSignIn(): Promise<KeycloakLoginPage> {
		await this.signInButton.click();

		return new KeycloakLoginPage(this.page);
	}
}
