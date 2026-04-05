import type { Locator, Page } from "@playwright/test";
import type { BaseWizard } from "./BaseWizard";
import { BoardWizard } from "./BoardWizard";
import { CsvUploadWizard } from "./CsvUploadWizard";

export abstract class BaseSettingsPage<T> {
	page: Page;
	createPageHandler: (page: Page) => T;

	constructor(page: Page, createPageHandler: (page: Page) => T) {
		this.page = page;
		this.createPageHandler = createPageHandler;
	}

	async selectWizard(
		workTrackingSystemType: string,
		wizardType: "File",
	): Promise<CsvUploadWizard<T>>;

	async selectWizard(
		workTrackingSystemType: string,
		wizardType?: "Board" | "Team",
	): Promise<BoardWizard<T>>;

	async selectWizard(
		workTrackingSystemType: string,
		wizardType: "Board" | "Team" | "File" = "Board",
	): Promise<BaseWizard<T>> {
		await this.page
			.getByRole("button", {
				name: `Select ${workTrackingSystemType} ${wizardType}`,
			})
			.click();

		if (wizardType === "File") {
			return new CsvUploadWizard(this.page, this.createPageHandler);
		}
		return new BoardWizard(this.page, this.createPageHandler, wizardType);
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
}
