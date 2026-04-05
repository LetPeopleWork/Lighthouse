import type { Locator, Page } from "@playwright/test";
import { BaseWizard } from "./BaseWizard";

export class BoardWizard<T> extends BaseWizard<T> {
	constructor(
		page: Page,
		createPageHandler: (page: Page) => T,
		private readonly type: string,
	) {
		super(page, createPageHandler);
	}

	async selectByName(name: string): Promise<void> {
		await this.page.getByRole("combobox", { name: this.type }).click();
		await this.page.getByRole("option", { name }).click();
	}

	async confirm(): Promise<T> {
		await this.confirmButton.click();
		return this.createPageHandler(this.page);
	}

	get boardInformationPanel(): Locator {
		return this.page.getByText("Board InformationQuery");
	}

	get confirmButton(): Locator {
		return this.page.getByRole("button", { name: "Confirm" });
	}
}
