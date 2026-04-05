import type { Locator } from "@playwright/test";
import { BaseSettingsPage } from "./BaseSettingsPage";

export abstract class BaseEditPage<T> extends BaseSettingsPage<T> {
	abstract save(): Promise<T>;

	async setName(newName: string): Promise<void> {
		await this.page.getByLabel("Name", { exact: true }).fill(newName);
	}

	async getName(): Promise<string> {
		return (
			(await this.page.getByLabel("Name", { exact: true }).inputValue()) ?? ""
		);
	}

	async toggleAdvancedConfiguration(): Promise<void> {
		await this.page
			.getByRole("heading", { name: "Advanced Configuration" })
			.click();
	}

	async getSelectedParentOverride(): Promise<string> {
		const combobox = this.parentOverrideCombobox;
		return (await combobox.textContent()) ?? "";
	}

	async selectParentOverride(additionalField: string): Promise<void> {
		await this.parentOverrideCombobox.click();
		await this.page.getByRole("option", { name: additionalField }).click();
	}

	async getPotentialParentOverrides(): Promise<string[]> {
		await this.parentOverrideCombobox.click();
		const options = await this.page.getByRole("option").allInnerTexts();
		await this.page.keyboard.press("Escape");
		return options;
	}

	async setEstimationField(estimationField: string): Promise<void> {
		await this.page.getByRole("heading", { name: "Estimation" }).click();

		await this.estimationFieldCombobox.click();
		await this.page.getByRole("option", { name: estimationField }).click();

		await this.page
			.getByRole("textbox", { name: "Estimation Unit" })
			.fill(estimationField);
	}

	get estimationFieldCombobox(): Locator {
		return this.page
			.locator("div")
			.filter({ hasText: /.*Estimation Field$/ })
			.getByRole("combobox");
	}

	get parentOverrideCombobox(): Locator {
		return this.page
			.locator("div")
			.filter({ hasText: /.*Parent Override Field$/ })
			.getByRole("combobox");
	}

	get saveButton(): Locator {
		return this.page.getByRole("button", { name: "Save" });
	}
}
