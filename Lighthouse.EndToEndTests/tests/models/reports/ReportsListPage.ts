import type { Locator, Page } from "@playwright/test";
import { formatLocalDate } from "../../helpers/dates";
import { ReportPage } from "./ReportPage";

/** What a Then & Now report is created with; anything left out stays as the wizard opens it. */
export interface ThenAndNowChoices {
	thenEndsOn?: Date;
	thenDays?: number;
	name?: string;
}

/**
 * The Reports tab of a Team or a Portfolio, and the wizard that creates a report from it. Left as it
 * opens, the wizard creates a Then & Now report whose Then is the 90 days ending today, with Now as long
 * and all four metrics.
 */
export class ReportsListPage {
	constructor(private readonly page: Page) {}

	/** The list marks itself ready once it has read the reports; until then Create Report waits too. */
	get ready(): Locator {
		return this.page.locator(
			'[data-testid="reports-list"][data-widget-status="ready"]',
		);
	}

	async waitUntilReady(): Promise<ReportsListPage> {
		await this.ready.waitFor();
		return this;
	}

	get createReportButton(): Locator {
		return this.page.getByRole("button", { name: "Create Report" });
	}

	get wizard(): Locator {
		return this.page.getByRole("dialog");
	}

	reportLink(name: string): Locator {
		return this.page.getByRole("link", { name });
	}

	/** Opens the wizard on its Type step. */
	async openCreateReport(): Promise<Locator> {
		await this.waitUntilReady();
		await this.createReportButton.click();
		await this.wizard
			.getByText(/Set today's flow against a window that stays as it was\./)
			.waitFor();
		return this.wizard;
	}

	/** From the Type step to the Then step, which names the days its window covers. */
	async goToThenStep(): Promise<Locator> {
		await this.nextStep();
		await this.wizard.getByText(/^= \d{1,2} \w{3,4}/).waitFor();
		return this.wizard;
	}

	async createTheDefaultReport(): Promise<ReportPage> {
		return this.createReport({});
	}

	async createReport(choices: ThenAndNowChoices): Promise<ReportPage> {
		await this.openCreateReport();
		await this.goToThenStep();

		if (choices.thenEndsOn !== undefined) {
			await this.wizard
				.getByLabel("Then ends on")
				.fill(formatLocalDate(choices.thenEndsOn));
		}
		if (choices.thenDays !== undefined) {
			await this.wizard
				.getByLabel("Then length in days")
				.fill(String(choices.thenDays));
		}

		await this.nextStep();
		await this.wizard.getByText(/always ending today/).waitFor();
		await this.nextStep();
		if (choices.name !== undefined) {
			await this.wizard.getByLabel("Name", { exact: true }).fill(choices.name);
		}

		await this.wizard
			.getByRole("button", { name: "Create", exact: true })
			.click();
		await this.page.waitForURL(/\/(teams|portfolios)\/\d+\/reports\/\d+$/);
		return new ReportPage(this.page).waitUntilReady();
	}

	private async nextStep(): Promise<void> {
		await this.wizard.getByRole("button", { name: "Next" }).click();
	}
}
