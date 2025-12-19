import type { Page } from "@playwright/test";
import { DeliveriesPage } from "./DeliveriesPage";

export class ModifyDeliveriesDialog {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async close(): Promise<void> {
		const closeButton = this.page.getByRole("button").first();
		await closeButton.click();
	}

	async save(): Promise<DeliveriesPage> {
		const saveButton = this.page.getByRole("button", { name: "Save" });
		await saveButton.click();

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
}
