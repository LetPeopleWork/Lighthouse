import type { Locator, Page } from "@playwright/test";
import { LighthousePage } from "../app/LighthousePage";
import { OverviewPage } from "../overview/OverviewPage";

export class KeycloakLoginPage {
	readonly page: Page;
	readonly usernameInput: Locator;
	readonly passwordInput: Locator;
	readonly submitButton: Locator;

	constructor(page: Page) {
		this.page = page;
		this.usernameInput = page.locator("#username");
		this.passwordInput = page.locator("#password");
		this.submitButton = page.locator("#kc-login");
	}

	async login(username: string, password: string): Promise<OverviewPage> {
		await this.usernameInput.fill(username);
		await this.passwordInput.fill(password);
		await this.submitButton.click();

		return new OverviewPage(this.page, new LighthousePage(this.page));
	}
}
