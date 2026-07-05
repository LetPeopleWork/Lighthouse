import type { Locator, Page } from "@playwright/test";

export class ForecastFilterEditor {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get root(): Locator {
		// The team edit page hosts two shared DeliveryRuleBuilder instances
		// (Forecast Filter + Blocked). Scope to the forecast one by its title.
		return this.page
			.getByTestId("delivery-rule-builder")
			.filter({ hasText: "Exclude items where" });
	}

	get addRuleButton(): Locator {
		return this.root.getByTestId("add-rule-button");
	}

	get groupModeToggle(): Locator {
		return this.root.getByTestId("rule-group-mode-toggle");
	}

	fieldSelect(ruleIndex: number): Locator {
		return this.root.getByTestId(`rule-field-select-${ruleIndex}`);
	}

	operatorSelect(ruleIndex: number): Locator {
		return this.root.getByTestId(`rule-operator-select-${ruleIndex}`);
	}

	valueInput(ruleIndex: number): Locator {
		return this.root.getByTestId(`rule-value-input-${ruleIndex}`);
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
