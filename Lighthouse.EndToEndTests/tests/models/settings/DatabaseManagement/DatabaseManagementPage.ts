import path from "node:path";
import { expect, type Page } from "@playwright/test";

export class DatabaseManagementPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async getProvider(): Promise<string> {
		return (await this.page.getByTestId("database-provider").innerText())
			.split(": ")[1]
			.trim();
	}

	async createBackup(password: string): Promise<string> {
		const passwordInput = this.page.getByTestId("backup-password");
		await passwordInput.fill(password);

		const downloadPromise = this.page.waitForEvent("download");
		await this.page.getByTestId("backup-button").click();

		const download = await downloadPromise;
		const fileName = download.suggestedFilename();
		const filePath = path.join(process.cwd(), "temp-downloads", fileName);
		await download.saveAs(filePath);

		return filePath;
	}

	async restoreBackup(filePath: string, password: string): Promise<void> {
		const fileInput = this.page.getByTestId("restore-file-input");
		await fileInput.setInputFiles(filePath);

		const passwordInput = this.page.getByTestId("restore-password");
		await passwordInput.fill(password);

		await this.page.getByTestId("restore-button").click();

		await expect(
			this.page.getByRole("alert").filter({ hasText: "Restore completed" }),
		).toBeVisible();
	}

	async clearDatabase(): Promise<void> {
		await this.page.getByTestId("clear-button").click();
		await this.page.getByTestId("confirm-clear-button").click();

		await expect(
			this.page
				.getByRole("alert")
				.filter({ hasText: "Database cleared and reset" }),
		).toBeVisible();
	}
}
