import type { Locator, Page } from "@playwright/test";
import { ReportsListPage } from "./ReportsListPage";

/**
 * One report as a reader sees it. It is ready once the page marks the report itself as read; waiting for
 * mere visibility would accept the spinner shown while the report is on its way.
 */
export class ReportPage {
	constructor(private readonly page: Page) {}

	get ready(): Locator {
		return this.page.locator('[data-report-status="ready"]');
	}

	async waitUntilReady(): Promise<ReportPage> {
		await this.ready.waitFor();
		return this;
	}

	/** The report's name, the last part of the "Reports / <name>" line at the page's top. */
	get title(): Locator {
		return this.page
			.getByRole("navigation", { name: "breadcrumb" })
			.getByRole("listitem")
			.last();
	}

	async readName(): Promise<string> {
		return ((await this.title.textContent()) ?? "").trim();
	}

	/** The breadcrumb's "Reports" leads back to the list; the Reports tab is already selected here. */
	async backToReports(): Promise<ReportsListPage> {
		await this.page
			.getByRole("navigation", { name: "breadcrumb" })
			.getByRole("link", { name: "Reports", exact: true })
			.click();
		await this.page.waitForURL(/\/(teams|portfolios)\/\d+\/reports$/);
		return new ReportsListPage(this.page).waitUntilReady();
	}

	panel(title: string): Locator {
		return this.page.getByRole("region", { name: title });
	}
}
