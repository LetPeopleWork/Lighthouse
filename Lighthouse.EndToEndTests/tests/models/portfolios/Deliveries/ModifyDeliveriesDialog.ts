import type { Locator, Page } from "@playwright/test";
import { DeliveriesPage } from "./DeliveriesPage";

export class ModifyDeliveriesDialog {
	page: Page;

	saveButtonText: string = "Save";

	constructor(page: Page, isUpdate: boolean = false) {
		this.page = page;

		if (isUpdate) {
			this.saveButtonText = "Update";
		}
	}

	async close(): Promise<void> {
		const closeButton = this.page.getByRole("button").first();
		await closeButton.click();
	}

	async save(): Promise<DeliveriesPage> {
		await this.saveButton.click();

		return new DeliveriesPage(this.page);
	}

	async setDeliveryName(name: string): Promise<void> {
		const nameInput = this.page.getByRole("textbox", { name: "Delivery Name" });
		await nameInput.fill(name);
	}

	async hasDeliveryNameValidationError(): Promise<boolean> {
		const errorMessage = this.page.getByText("Delivery name is required");
		return await errorMessage.isVisible();
	}

	async setDeliveryDate(date: string): Promise<void> {
		const dateInput = this.page.getByRole("textbox", { name: "Delivery Date" });
		await dateInput.fill(date);
	}

	async hasDeliveryDateValidationError(): Promise<boolean> {
		const errorMessage = this.page.getByText("Delivery date is required");
		return await errorMessage.isVisible();
	}

	async selectFeatureByIndex(index: number): Promise<void> {
		const dataGrid = this.page.locator(".MuiDataGrid-root");
		const rows = dataGrid.locator(".MuiDataGrid-row");
		const targetRow = rows.nth(index);

		// More specific selector for the checkbox in the "selected" column
		const checkbox = targetRow.locator(
			'[data-field="selected"] input[type="checkbox"]',
		);
		await checkbox.click();
	}

	async hasAtLeastOneFeatureValidationError(): Promise<boolean> {
		const errorMessage = this.page.getByText("At least one feature must be");
		return await errorMessage.isVisible();
	}

	async hasRulesMustBeValidatedValidationError(): Promise<boolean> {
		const errorMessage = this.page.getByText(
			"Rules must be validated before saving",
		);
		return await errorMessage.isVisible();
	}

	// Rule-based delivery methods
	async switchToRuleBased(): Promise<void> {
		const ruleBasedButton = this.page.getByRole("button", {
			name: "Rule-Based",
		});
		await ruleBasedButton.click();
	}

	async switchToManual(): Promise<void> {
		const manualButton = this.page.getByRole("button", { name: "Manual" });
		await manualButton.click();
	}

	async addRule(): Promise<void> {
		const addRuleButton = this.page.getByRole("button", { name: "Add Rule" });
		await addRuleButton.click();
	}

	async setRuleField(ruleIndex: number, fieldValue: string): Promise<void> {
		const fieldSelect = this.page.locator(
			`[data-testid="rule-field-select-${ruleIndex}"]`,
		);
		await fieldSelect.click();
		await this.page.getByRole("option", { name: fieldValue }).click();
	}

	async setRuleOperator(
		ruleIndex: number,
		operatorValue: string,
	): Promise<void> {
		const operatorSelect = this.page.locator(
			`[data-testid="rule-operator-select-${ruleIndex}"]`,
		);
		await operatorSelect.click();
		await this.page
			.getByRole("option", { name: operatorValue, exact: true })
			.click();
	}

	async setRuleValue(ruleIndex: number, value: string): Promise<void> {
		const valueInput = this.page
			.getByRole("textbox", { name: "Value" })
			.nth(ruleIndex);
		await valueInput.fill(value);
	}

	async removeRule(ruleIndex: number): Promise<void> {
		const removeButton = this.page.locator(
			`[data-testid="rule-delete-${ruleIndex}"]`,
		);
		await removeButton.click();
	}

	async validateRules(): Promise<void> {
		const validateButton = this.page.getByRole("button", {
			name: "Validate Rules",
		});
		await validateButton.click();
	}

	get saveButton(): Locator {
		return this.page.getByRole("button", { name: this.saveButtonText });
	}
}
