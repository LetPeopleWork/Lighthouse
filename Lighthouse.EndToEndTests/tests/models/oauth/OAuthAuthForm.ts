import type { Locator, Page } from "@playwright/test";

export class OAuthAuthForm {
	private readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get clientIdField(): Locator {
		return this.page.getByLabel("Client ID");
	}

	get clientSecretField(): Locator {
		return this.page.getByLabel("Client Secret");
	}

	get callbackUrlField(): Locator {
		return this.page.getByLabel("Callback URL");
	}

	get connectButton(): Locator {
		return this.page.getByRole("button", { name: "Connect" });
	}

	get baseUrlWarning(): Locator {
		return this.page.getByText(
			/Set Lighthouse:BaseUrl in your server configuration/i,
		);
	}
}
