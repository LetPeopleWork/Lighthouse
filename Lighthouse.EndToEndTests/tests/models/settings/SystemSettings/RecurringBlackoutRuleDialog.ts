import type { Page } from "@playwright/test";
import { SystemConfigurationPage } from "./SystemConfigurationPage";

export interface RecurringRuleOptions {
	weekdays: string[];
	intervalWeeks: number;
	startDate: string;
	endDate?: string;
	description?: string;
}

export class RecurringBlackoutRuleDialog {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async configureRule(options: RecurringRuleOptions): Promise<void> {
		for (const weekday of options.weekdays) {
			await this.page.getByLabel(weekday, { exact: true }).check();
		}

		await this.page
			.getByTestId("recurring-interval-weeks")
			.locator("input")
			.fill(`${options.intervalWeeks}`);

		await this.page
			.getByTestId("recurring-start-date")
			.locator("input")
			.fill(options.startDate);

		if (options.endDate) {
			await this.page
				.getByTestId("recurring-end-date")
				.locator("input")
				.fill(options.endDate);
		}

		if (options.description) {
			await this.page
				.getByTestId("recurring-description")
				.locator("input")
				.fill(options.description);
		}
	}

	async saveRule(): Promise<SystemConfigurationPage> {
		await this.page.getByTestId("save-recurring-blackout-rule").click();
		return new SystemConfigurationPage(this.page);
	}
}
