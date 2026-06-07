import type { Locator, Page } from "@playwright/test";

export class WaitStatesEditor {
	constructor(public readonly page: Page) {}

	get configureWaitStatesToggle(): Locator {
		return this.page.getByLabel("Configure Wait States");
	}

	get addWaitStateInput(): Locator {
		return this.page.getByRole("combobox", { name: "New Wait State" });
	}

	get section(): Locator {
		return this.configureWaitStatesToggle.locator(
			'xpath=ancestor::div[contains(@class,"MuiCard-root")][1]',
		);
	}

	async enable(): Promise<void> {
		if (!(await this.configureWaitStatesToggle.isChecked())) {
			await this.configureWaitStatesToggle.check();
		}
	}

	async addWaitState(stateOrMappingName: string): Promise<void> {
		await this.addWaitStateInput.click();
		await this.addWaitStateInput.fill(stateOrMappingName);
		await this.page
			.getByRole("option", { name: stateOrMappingName, exact: true })
			.click();
	}

	waitStateChip(name: string): Locator {
		return this.page.locator(".MuiChip-root", { hasText: name });
	}
}
