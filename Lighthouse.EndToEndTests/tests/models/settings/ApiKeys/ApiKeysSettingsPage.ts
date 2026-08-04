import type { Locator, Page } from "@playwright/test";

export type ApiKeyScopeType = "System" | "Team" | "Portfolio";

/** The labels the scope editor shows, not the wire roles it maps them to. */
export type ApiKeyAccess =
	| "Read access"
	| "Write access"
	| "System administrator";

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

	get createdKeyValue(): Locator {
		return this.page.getByTestId("created-api-key-value");
	}

	get doneButton(): Locator {
		return this.page.getByTestId("api-key-done-button");
	}

	scopeRow(index: number): Locator {
		return this.page.getByTestId(`scope-row-${index}`);
	}

	/**
	 * Fills one scope row. The three selects are order-dependent in the product:
	 * picking a scope type resets access and target, and access stays disabled
	 * until a scope type exists — so type, then access, then target.
	 */
	async setScopeRow(
		index: number,
		scope: {
			scopeType: ApiKeyScopeType;
			access: ApiKeyAccess;
			target?: string;
		},
	): Promise<void> {
		await this.chooseInScopeRow(index, "Scope type", scope.scopeType);
		await this.chooseInScopeRow(index, "Access", scope.access);

		if (scope.target !== undefined) {
			await this.chooseInScopeRow(index, "Target", scope.target);
		}
	}

	/**
	 * Submits the dialog and returns the plaintext key.
	 *
	 * This is the only moment the key is ever readable: the backend stores a
	 * salted PBKDF2/SHA-256 hash (ApiKeyService.HashKey), so it cannot be read
	 * back afterwards and cannot be seeded into the database without
	 * reimplementing the KDF in test setup.
	 */
	async createAndRevealKey(): Promise<string> {
		await this.submitButton.click();
		await this.createdKeyValue.waitFor({ state: "visible" });

		const key = await this.createdKeyValue.innerText();
		return key.trim();
	}

	async clickDone(): Promise<void> {
		await this.doneButton.click();
		await this.dialog.waitFor({ state: "hidden" });
	}

	private async chooseInScopeRow(
		index: number,
		selectLabel: "Scope type" | "Access" | "Target",
		optionName: string,
	): Promise<void> {
		await this.scopeRow(index)
			.getByRole("combobox", { name: selectLabel })
			.click();
		await this.page
			.getByRole("option", { name: optionName, exact: true })
			.click();
	}
}
