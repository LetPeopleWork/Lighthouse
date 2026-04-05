import { BaseWizard } from "./BaseWizard";

export class CsvUploadWizard<T> extends BaseWizard<T> {
	async selectByName(filePath: string): Promise<void> {
		await this.page.waitForSelector('input[type="file"]', { timeout: 3000 });
		await this.page.locator('input[type="file"]').setInputFiles(filePath);
	}

	async confirm(): Promise<T> {
		await this.waitForUploadComplete();
		await this.page.getByRole("button", { name: "Use File" }).click();
		return this.createPageHandler(this.page);
	}

	async isFileUploadVisible(): Promise<boolean> {
		try {
			await this.page.waitForSelector("text=Select CSV File", {
				timeout: 1000,
			});
			return true;
		} catch {
			return false;
		}
	}

	async waitForUploadComplete(): Promise<void> {
		try {
			await this.page.waitForSelector("text=Processing:", {
				state: "hidden",
				timeout: 1000,
			});
		} catch {
			// Processing indicator may not appear for small files
		}
	}

	async hasValidationErrors(): Promise<boolean> {
		try {
			await this.page.waitForSelector("text=Validation Errors:", {
				timeout: 1000,
			});
			return true;
		} catch {
			return false;
		}
	}

	async getValidationErrors(): Promise<string[]> {
		if (!(await this.hasValidationErrors())) return [];
		const errors = await this.page.locator("text=/^• .*$/").allTextContents();
		return errors.map((e) => e.replace(/^• /, ""));
	}
}
