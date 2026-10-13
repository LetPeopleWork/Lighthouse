import type { Locator, Page } from "@playwright/test";
import { ReportPage } from "./ReportPage";

/**
 * The Reports tab of a Team or a Portfolio, and the wizard that creates a report from it. The wizard is
 * walked with every step left as it opens, which for Then & Now is Then the 90 days ending today, Now as
 * long, and all four metrics.
 */
export class ReportsListPage {
	constructor(private readonly page: Page) {}

	get createReportButton(): Locator {
		return this.page.getByRole("button", { name: "Create Report" });
	}

	get wizard(): Locator {
		return this.page.getByRole("dialog");
	}

	reportLink(name: string): Locator {
		return this.page.getByRole("link", { name });
	}

	async createTheDefaultReport(): Promise<ReportPage> {
		await this.createReportButton.click();
		// Each step is waited for by what it says, so a step that never appears fails here, not later.
		const whatEachStepSays = [
			/Set today's flow against a window that stays as it was\./,
			/frozen when you create it/,
			/always ending today/,
		];
		for (const says of whatEachStepSays) {
			await this.wizard.getByText(says).waitFor();
			await this.wizard.getByRole("button", { name: "Next" }).click();
		}
		await this.wizard
			.getByRole("button", { name: "Create", exact: true })
			.click();
		await this.page.waitForURL(/\/(teams|portfolios)\/\d+\/reports\/\d+$/);
		return new ReportPage(this.page).waitUntilReady();
	}
}
