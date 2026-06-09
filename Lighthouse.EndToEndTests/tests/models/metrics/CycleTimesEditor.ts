import type { Locator, Page } from "@playwright/test";

export class CycleTimesEditor {
	constructor(public readonly page: Page) {}

	get section(): Locator {
		return this.page
			.getByText("Cycle Times", { exact: true })
			.locator('xpath=ancestor::div[contains(@class,"MuiCard-root")][1]');
	}

	definitionRow(name: string): Locator {
		return this.section.locator(".MuiCard-root", { hasText: name });
	}
}
