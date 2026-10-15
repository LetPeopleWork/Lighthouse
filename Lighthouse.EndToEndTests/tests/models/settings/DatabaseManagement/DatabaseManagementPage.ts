import path from "node:path";
import { expect, type Locator, type Page } from "@playwright/test";

export class DatabaseManagementPage {
	page: Page;

	private static readonly restoreDeadlineMs = 60_000;

	constructor(page: Page) {
		this.page = page;
	}

	private get restoreCompleted(): Locator {
		return this.page
			.getByRole("alert")
			.filter({ hasText: "Restore completed" })
			.first();
	}

	private get refusedWhileHistoryFills(): Locator {
		return this.page
			.getByRole("alert")
			.filter({ hasText: "filling in days it was missing" })
			.first();
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
		// A chart opened by an earlier test may still be filling in past days, and the app
		// refuses database operations until that has finished, so a refused restore is tried again.
		const giveUpAt = Date.now() + DatabaseManagementPage.restoreDeadlineMs;

		while (!(await this.tryRestore(filePath, password))) {
			if (Date.now() > giveUpAt) {
				throw new Error(
					"The restore was still refused after 60 s because a chart was filling in past days.",
				);
			}

			await this.page.waitForTimeout(1_000);
			await this.page.reload();
			await this.page.getByTestId("database-tab").click();
		}
	}

	private async tryRestore(
		filePath: string,
		password: string,
	): Promise<boolean> {
		await this.page.getByTestId("database-provider").waitFor();
		if (await this.refusedWhileHistoryFills.isVisible()) {
			return false;
		}

		await this.page.getByTestId("restore-file-input").setInputFiles(filePath);
		await this.page.getByTestId("restore-password").fill(password);
		await this.page.getByTestId("restore-button").click();

		await expect(
			this.restoreCompleted.or(this.refusedWhileHistoryFills).first(),
		).toBeVisible();

		return this.restoreCompleted.isVisible();
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
