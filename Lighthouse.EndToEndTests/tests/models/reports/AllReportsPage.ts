import type { Locator, Page } from "@playwright/test";
import { ReportPage } from "./ReportPage";

/** A Team or Portfolio as the Reports page names it: the name the user gave it, and its kind in the instance's words. */
export interface ReportOwnerName {
	readonly name: string;
	readonly kind: string;
}

/**
 * The Reports page: every report on every Team and Portfolio the reader can read, in one table, and the wizard
 * that creates one report for several owners at once. Left as it opens, the wizard creates a Then & Now report of
 * 90 days of Then with Now as long, back to back and ending today, with all four metrics.
 */
export class AllReportsPage {
	constructor(private readonly page: Page) {}

	/** The table marks itself ready once it has read the reports. */
	get ready(): Locator {
		return this.page.locator(
			'[data-testid="all-reports"][data-widget-status="ready"]',
		);
	}

	async waitUntilReady(): Promise<AllReportsPage> {
		await this.ready.waitFor();
		return this;
	}

	get createReportsButton(): Locator {
		return this.page.getByRole("button", { name: "Create reports" });
	}

	get dialog(): Locator {
		return this.page.getByRole("dialog");
	}

	/** The Owner cell shows the name alone; its link is announced with the kind, e.g. "Team Zenith, Team". */
	ownerLink(owner: ReportOwnerName): Locator {
		return this.page.getByRole("link", {
			name: `${owner.name}, ${owner.kind}`,
			exact: true,
		});
	}

	rowOf(reportName: string, owner: ReportOwnerName): Locator {
		return this.page
			.getByRole("row")
			.filter({
				has: this.page.getByRole("link", { name: reportName, exact: true }),
			})
			.filter({ has: this.ownerLink(owner) });
	}

	/** Creates one report, with the wizard's window as it opens, for every owner named as its checkbox reads. */
	async createForEach(
		owners: readonly ReportOwnerName[],
		reportName: string,
	): Promise<AllReportsPage> {
		await this.waitUntilReady();
		await this.createReportsButton.click();
		// The wizard opens only once what a report needs has arrived, so each step is waited for by its own words.
		await this.dialog
			.getByText(/Set today's flow against a window that stays as it was\./)
			.waitFor();
		await this.nextStep();
		await this.dialog.getByText(/^= \d{1,2} \w{3,4}/).waitFor();
		await this.nextStep();
		await this.dialog.getByText(/always ending today/).waitFor();
		await this.nextStep();
		await this.dialog.getByLabel("Name", { exact: true }).fill(reportName);
		await this.nextStep();
		await this.dialog
			.getByText("Step 5 of 5 — Who gets this report?")
			.waitFor();

		for (const owner of owners) {
			await this.dialog.getByRole("checkbox", { name: owner.name }).check();
		}
		await this.dialog
			.getByRole("button", { name: `Create ${owners.length} reports` })
			.click();
		await this.dialog
			.getByText(`${owners.length} reports created, 0 not created`)
			.waitFor();
		await this.dialog.getByRole("button", { name: "Done" }).click();
		return this.waitUntilReady();
	}

	async open(reportName: string, owner: ReportOwnerName): Promise<ReportPage> {
		await this.rowOf(reportName, owner)
			.getByRole("link", { name: reportName, exact: true })
			.click();
		await this.page.waitForURL(/\/(teams|portfolios)\/\d+\/reports\/\d+$/);
		return new ReportPage(this.page).waitUntilReady();
	}

	private async nextStep(): Promise<void> {
		await this.dialog.getByRole("button", { name: "Next" }).click();
	}
}
