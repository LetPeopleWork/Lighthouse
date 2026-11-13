import type { Locator, Page } from "@playwright/test";
import { BlackoutPeriodDialog } from "./BlackoutPeriodDialog";
import { RecurringBlackoutRuleDialog } from "./RecurringBlackoutRuleDialog";

/** Settings > Organization > Calendar. */
export class CalendarPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async addBlackoutPeriod(): Promise<BlackoutPeriodDialog> {
		await this.page
			.getByRole("button", { name: "Add Blackout Period" })
			.click();

		return new BlackoutPeriodDialog(this.page);
	}

	async addRecurringRule(): Promise<RecurringBlackoutRuleDialog> {
		await this.page.getByRole("button", { name: "Add Recurring Rule" }).click();

		return new RecurringBlackoutRuleDialog(this.page);
	}

	get blackoutPeriodsSection(): Locator {
		return this.page.getByText(
			"Blackout Periods & Recurring RulesDefine global",
		);
	}
}
