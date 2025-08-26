import type { Page } from "@playwright/test";

/**
 * Page object methods for CSV file upload functionality
 */
export class CsvUploadHelper {
	constructor(private page: Page) {}

	/**
	 * Uploads a CSV file through the file input
	 * @param filePath - Path to the CSV file to upload
	 */
	async uploadCsvFile(filePath: string): Promise<void> {
		// Wait for file input to be available
		await this.page.waitForSelector('input[type="file"]', { timeout: 10000 });
		const fileInput = this.page.locator('input[type="file"]');
		await fileInput.setInputFiles(filePath);
	}

	/**
	 * Checks if the file upload component is visible
	 */
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

	/**
	 * Gets the selected file name if any
	 */
	async getSelectedFileName(): Promise<string | null> {
		try {
			// Look for the file name in the selected file section
			const selectedFileElement = await this.page
				.locator("text=/Selected file:\\s*(.+?)\\s/")
				.first();
			if (await selectedFileElement.isVisible({ timeout: 2000 })) {
				const text = await selectedFileElement.textContent();
				if (text) {
					// Extract file name from "Selected file: filename.csv" text
					const match = text.match(/Selected file:\s*(.+?)(\s|$)/);
					return match ? match[1] : null;
				}
			}
		} catch {
			// No file selected or element not found
		}
		return null;
	}

	/**
	 * Waits for file upload to complete (no processing indicator visible)
	 */
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

	/**
	 * Checks if there are validation errors
	 */
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

	/**
	 * Gets validation error messages
	 */
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
