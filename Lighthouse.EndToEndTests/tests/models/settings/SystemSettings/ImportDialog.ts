import type { Locator, Page } from "@playwright/test";
import { SystemConfigurationPage } from "./SystemConfigurationPage";

export class ImportDialog {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async selectFile(filePath: string): Promise<void> {
		const fileInput = this.page.locator('input[type="file"]');
		await fileInput.setInputFiles(filePath, { noWaitAfter: true });
		await this.page.waitForTimeout(1000);
	}

	async getImportElementStatus(elementName: string): Promise<string> {
		const elementFilter = `${elementName} {0}`;

		const findElement = (status: string) => {
			return this.page
				.getByTestId("validation-results")
				.locator("div")
				.filter({ hasText: elementFilter.replace("{0}", status) })
				.locator("span");
		};

		const updateElement = findElement("Update");
		const newElement = findElement("New");
		const errorElement = findElement("Error");

		if (await updateElement.isVisible()) {
			return "Update";
		}

		if (await newElement.isVisible()) {
			return "New";
		}

		if (await errorElement.isVisible()) {
			return "Error";
		}

		return "Unknown";
	}

	async hasError(): Promise<boolean> {
		const errorIcon = this.page.getByRole("alert").locator("svg");
		return await errorIcon.isVisible();
	}

	async wasSuccess(): Promise<boolean> {
		const successMessage = this.page.getByText("All items were successfully");
		return await successMessage.isVisible();
	}

	async goToNextStep(): Promise<void> {
		return await this.nextButton.click();
	}

	get nextButton(): Locator {
		return this.page.getByRole("button", { name: "Next" });
	}

	async import(): Promise<void> {
		return this.page.getByRole("button", { name: "Import" }).click();
	}

	async waitForImportToFinish(): Promise<void> {
		let importDone = false;
		while (!importDone) {
			await this.page.waitForTimeout(1000);

			importDone = await this.page
				.getByRole("heading", { name: "Import Completed" })
				.isVisible();
		}
	}

	async close(): Promise<SystemConfigurationPage> {
		await this.page.getByRole("button", { name: "Close" }).click();

		return new SystemConfigurationPage(this.page);
	}

	async toggleClearConfiguration(): Promise<void> {
		await this.page
			.getByRole("checkbox", { name: "Delete Existing Configuration" })
			.click();
		await this.page.waitForTimeout(500);
	}

	async addSecretParameter(
		optionName: string,
		secretValue: string,
	): Promise<void> {
		await this.page
			.getByRole("textbox", { name: optionName, exact: false })
			.fill(secretValue);
	}

	async validate(): Promise<void> {
		const validateButton = this.page.getByRole("button", { name: "Validate" });
		await validateButton.click();
	}
}
