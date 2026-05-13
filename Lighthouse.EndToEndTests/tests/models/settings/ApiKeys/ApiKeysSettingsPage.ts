import type { Locator, Page } from "@playwright/test";

export class ApiKeysSettingsPage {
	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get panel(): Locator {
		return this.page.getByTestId("api-keys-panel");
	}

	get createButton(): Locator {
		return this.page.getByTestId("create-api-key-button");
	}

	get disabledMessage(): Locator {
		return this.page.getByTestId("api-keys-disabled-message");
	}

	get noKeysMessage(): Locator {
		return this.page.getByTestId("no-api-keys-message");
	}

	async openCreateDialog(): Promise<ApiKeyCreateDialog> {
		await this.createButton.click();
		const dialog = new ApiKeyCreateDialog(this.page);
		await dialog.dialog.waitFor({ state: "visible" });
		return dialog;
	}
}

export class ApiKeyCreateDialog {
	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get dialog(): Locator {
		return this.page.getByRole("dialog");
	}

	get nameInput(): Locator {
		return this.page.getByTestId("api-key-name-input");
	}

	get descriptionInput(): Locator {
		return this.page.getByTestId("api-key-description-input");
	}

	get scopeAccordion(): Locator {
		return this.page.getByTestId("scope-accordion");
	}

	get scopeAccordionSummary(): Locator {
		return this.page.getByTestId("scope-accordion-summary");
	}

	get scopeRowList(): Locator {
		return this.page.getByTestId("scope-row-list");
	}

	get addScopeRowButton(): Locator {
		return this.page.getByTestId("scope-row-list-add-button");
	}

	get submitButton(): Locator {
		return this.page.getByTestId("create-api-key-submit-button");
	}

	async setName(name: string): Promise<void> {
		await this.nameInput.fill(name);
	}

	async expandScopes(): Promise<void> {
		await this.scopeAccordionSummary.click();
		await this.scopeRowList.waitFor({ state: "visible" });
	}

	async addScopeRow(): Promise<void> {
		await this.addScopeRowButton.click();
	}
}
