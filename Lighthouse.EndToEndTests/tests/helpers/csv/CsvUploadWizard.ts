import type { Page } from "@playwright/test";
import type { PortfolioEditPage } from "../../models/portfolios/PortfolioEditPage";
import type { TeamEditPage } from "../../models/teams/TeamEditPage";

/**
 * Page object methods for CSV file upload functionality
 */
export class CsvUploadWizard<T extends PortfolioEditPage | TeamEditPage> {
	constructor(
		private readonly page: Page,
		private readonly createPageHandler: (page: Page) => T,
	) {}

	async uploadCsvFile(filePath: string): Promise<void> {
		// Wait for file input to be available
		await this.page.waitForSelector('input[type="file"]', { timeout: 10000 });
		const fileInput = this.page.locator('input[type="file"]');
		await fileInput.setInputFiles(filePath);
	}

	async isFileUploadVisible(): Promise<boolean> {
		try {
			await this.page.waitForSelector("text=Upload CSV File", {
				timeout: 5000,
			});
			return true;
		} catch {
			return false;
		}
	}

	async waitForUploadComplete(): Promise<void> {
		// Wait for processing indicator to disappear if it exists
		try {
			await this.page.waitForSelector("text=Processing:", {
				state: "hidden",
				timeout: 30000,
			});
		} catch {
			// Processing indicator may not appear for small files
		}
	}

	async useFile(): Promise<T> {
		await this.page.getByRole("button", { name: "Use File" }).click();
		return this.createPageHandler(this.page);
	}

	async hasValidationErrors(): Promise<boolean> {
		try {
			await this.page.waitForSelector("text=Validation Errors:", {
				timeout: 2000,
			});
			return true;
		} catch {
			return false;
		}
	}

	async getValidationErrors(): Promise<string[]> {
		const hasErrors = await this.hasValidationErrors();
		if (!hasErrors) {
			return [];
		}

		const errorElements = await this.page
			.locator("text=/^• .*$/")
			.allTextContents();
		return errorElements.map((error) => error.replace(/^• /, ""));
	}
}
