import type { Locator, Page } from "@playwright/test";

export class ForecastFilterEditor {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get root(): Locator {
		return this.page.getByTestId("delivery-rule-builder");
	}

	get addRuleButton(): Locator {
		return this.page.getByTestId("add-rule-button");
	}

	get groupModeToggle(): Locator {
		return this.page.getByTestId("rule-group-mode-toggle");
	}

	get takeEffectHint(): Locator {
		return this.page.getByTestId("forecast-filter-takeeffect-hint");
	}

	fieldSelect(ruleIndex: number): Locator {
		return this.page.getByTestId(`rule-field-select-${ruleIndex}`);
	}

	operatorSelect(ruleIndex: number): Locator {
		return this.page.getByTestId(`rule-operator-select-${ruleIndex}`);
	}

	valueInput(ruleIndex: number): Locator {
		return this.page.getByTestId(`rule-value-input-${ruleIndex}`);
	}

	async addExcludeByTypeRule(typeValue: string): Promise<void> {
		await this.addRuleButton.click();

		const ruleRow = this.root.getByTestId("rule-row").last();
		const ruleIndex = (await this.root.getByTestId("rule-row").count()) - 1;

		await this.fieldSelect(ruleIndex).click();
		await this.page.getByRole("option", { name: "Type", exact: true }).click();

		await this.operatorSelect(ruleIndex).click();
		await this.page
			.getByRole("option", { name: "Equals", exact: true })
			.click();

		const input = ruleRow.locator("input[type='text']").first();
		await input.fill(typeValue);
	}
}
