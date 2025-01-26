/// <reference lib="dom" />
import type { Page } from "@playwright/test";

export type LogLevels =
	| "Verbose"
	| "Debug"
	| "Information"
	| "Warning"
	| "Error"
	| "Fatal";

export class LogsPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async setLogLevel(level: LogLevels): Promise<void> {
		const currentLogLevel = await this.getCurrentLogLevel();
		await this.page.getByText(currentLogLevel, { exact: true }).click();

		await this.page.getByTestId(level).click();
	}

	async getCurrentLogLevel(): Promise<LogLevels> {
		const selectedOption = await this.page.$eval(
			"select#logLevel",
			(select: HTMLSelectElement) => select.value,
		);
		return selectedOption as LogLevels;
	}

	async download(): Promise<void> {
		await this.page.getByRole("button", { name: "Download" }).click();
	}

	async refresh(): Promise<void> {
		await this.page.getByRole("button", { name: "Refresh" }).click();
	}
}
