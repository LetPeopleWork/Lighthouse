import type { Locator, Page } from "@playwright/test";

export abstract class BaseEditPage<T> {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async validate(): Promise<void> {
		await this.validateButton.click();
		await this.validateButton.isEnabled();
	}

	abstract save(): Promise<T>;

	async setName(newName: string): Promise<void> {
		await this.page.getByLabel("Name", { exact: true }).fill(newName);
	}

	async getName(): Promise<string> {
		return (
			(await this.page.getByLabel("Name", { exact: true }).inputValue()) ?? ""
		);
	}

	async setDataRetrievalValue(
		dataRetrievalValue: string,
		dataRetrievalKey: string,
	): Promise<void> {
		await this.page.getByLabel(dataRetrievalKey).fill(dataRetrievalValue);
	}

	async getDataRetrievalValue(dataRetrievalKey: string): Promise<string> {
		return (
			(await this.page
				.getByLabel(dataRetrievalKey, { exact: true })
				.inputValue()) ?? ""
		);
	}

	async addTag(tag: string): Promise<void> {
		await this.page.getByLabel("New Tag").fill(tag);
		await this.page.keyboard.press("Enter");

		// Reset the input field
		await this.page.keyboard.press("Escape");
	}

	async removeTag(tag: string): Promise<void> {
		await this.removeChipItem(tag);
	}

	getTag(tag: string): Locator {
		return this.page.getByRole("button", { name: tag, exact: true });
	}

	async removeWorkItemType(workItemType: string): Promise<void> {
		await this.removeChipItem(workItemType);
	}

	async addWorkItemType(workItemType: string): Promise<void> {
		await this.page.getByLabel("New Work Item Type").fill(workItemType);
		await this.page.keyboard.press("Enter");

		// Reset the input field
		await this.page.keyboard.press("Escape");
	}

	async removeChipItem(itemText: string): Promise<void> {
		const chip = this.page.locator(".MuiChip-root", { hasText: itemText });
		await chip.locator(".MuiChip-deleteIcon").click();
	}

	getWorkItemType(workItemType: string): Locator {
		return this.page.getByRole("button", { name: workItemType });
	}

	async addState(
		state: string,
		stateCategory: "To Do" | "Doing" | "Done",
	): Promise<void> {
		await this.page.getByLabel(`New ${stateCategory} States`).fill(state);
		await this.page.keyboard.press("Enter");

		// Reset the input field
		await this.page.keyboard.press("Escape");
	}

	async removeState(state: string): Promise<void> {
		await this.removeChipItem(state);
	}

	async getWorkItemTypes(): Promise<string[]> {
		const section = this.page
			.locator(".MuiCard-root")
			.filter({
				has: this.page.getByRole("heading", {
					name: "Work Item Types",
					exact: true,
					level: 6,
				}),
			})
			.first();

		return section.locator(".MuiChip-label").allTextContents();
	}

	async getItemsFromStateSubsection(
		subsectionTitle: string,
	): Promise<string[]> {
		// Find the h6 heading with the subsection title
		const subsectionHeading = this.page.getByRole("heading", {
			name: subsectionTitle,
			exact: true,
			level: 6,
		});

		// Navigate to the parent container that holds the chips for this subsection
		// The chips are in a sibling div after the h6
		const subsectionContainer = subsectionHeading.locator(".."); // Go to parent div

		// Get chips only from this specific subsection
		const chipLabels = subsectionContainer.locator(".MuiChip-label");

		return chipLabels.allTextContents();
	}

	async getToDoStates(): Promise<string[]> {
		return this.getItemsFromStateSubsection("To Do");
	}

	async getDoingStates(): Promise<string[]> {
		return this.getItemsFromStateSubsection("Doing");
	}

	async getDoneStates(): Promise<string[]> {
		return this.getItemsFromStateSubsection("Done");
	}

	getState(state: string): Locator {
		return this.page.getByRole("button", { name: state, exact: true });
	}

	async selectWorkTrackingSystem(
		workTrackingSystemName: string,
	): Promise<void> {
		const combobox = this.page.locator(".MuiSelect-select").first();
		await combobox.click();

		await this.page
			.getByRole("option", { name: workTrackingSystemName })
			.click();
	}

	async resetWorkItemTypes(existingTypes: string[], newTypes: string[]) {
		for (const existingType of existingTypes) {
			await this.removeWorkItemType(existingType);
		}

		for (const itemType of newTypes) {
			await this.addWorkItemType(itemType);
		}
	}

	async resetStates(
		existingStates: { toDo: string[]; doing: string[]; done: string[] },
		newStates: { toDo: string[]; doing: string[]; done: string[] },
	) {
		for (const state of existingStates.toDo
			.concat(existingStates.doing)
			.concat(existingStates.done)) {
			await this.removeWorkItemType(state);
		}

		for (const state of newStates.toDo) {
			await this.addState(state, "To Do");
		}

		for (const state of newStates.doing) {
			await this.addState(state, "Doing");
		}

		for (const state of newStates.done) {
			await this.addState(state, "Done");
		}
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

	get validateButton(): Locator {
		return this.page.getByRole("button", { name: "Validate" });
	}
}
