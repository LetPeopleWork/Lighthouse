import type { Locator, Page } from "@playwright/test";
import { ReportPage } from "./ReportPage";

/** A Team or Portfolio as the Reports page names it: the name the user gave it, and its kind in the instance's words. */
export interface ReportOwnerName {
	readonly name: string;
	readonly kind: string;
}

/** The Owner cell shows the name alone; its link is announced with the kind, e.g. "Team Zenith, Team". */
const ownerLinkName = (owner: ReportOwnerName) => ({
	name: `${owner.name}, ${owner.kind}`,
	exact: true,
});

/**
 * The Reports page: every report on every Team and Portfolio the reader can read, in one table, and the wizard
 * that creates one report for several owners at once. Left as it opens, the wizard creates a Then & Now report of
 * 90 days of Then with Now as long, back to back and ending today, with all four metrics.
 */
export class AllReportsPage {
	constructor(private readonly page: Page) {}

	get table(): Locator {
		return this.page.getByTestId("all-reports");
	}

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

	ownerLink(owner: ReportOwnerName): Locator {
		return this.table.getByRole("link", ownerLinkName(owner));
	}

	// A `has` locator is resolved inside each row, so it starts from the page rather than from the table.
	rowOf(reportName: string, owner: ReportOwnerName): Locator {
		return this.table
			.getByRole("row")
			.filter({
				has: this.page.getByRole("link", { name: reportName, exact: true }),
			})
			.filter({ has: this.page.getByRole("link", ownerLinkName(owner)) });
	}

	/** Opens the wizard and goes through its steps as they come, to the one that asks who gets the report. */
	async openBulkWizardAtOwners(reportName: string): Promise<void> {
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
	}

	async pickOwners(owners: readonly ReportOwnerName[]): Promise<void> {
		for (const owner of owners) {
			await this.dialog.getByRole("checkbox", { name: owner.name }).check();
		}
	}

	/** The line saying why an owner cannot be picked: how far back its kept history reaches. */
	get keptHistoryNote(): Locator {
		return this.dialog.getByText(/Kept history starts/);
	}

	/** Creates for the owners picked and waits for the result to say how many were and were not created. */
	async createPicked(
		pickedCount: number,
		created: number,
		notCreated: number,
	): Promise<void> {
		await this.dialog
			.getByRole("button", { name: `Create ${pickedCount} reports` })
			.click();
		await this.dialog
			.getByText(`${created} reports created, ${notCreated} not created`)
			.waitFor();
	}

	/** Creates one report, with the wizard's window as it opens, for every owner named as its checkbox reads. */
	async createForEach(
		owners: readonly ReportOwnerName[],
		reportName: string,
	): Promise<AllReportsPage> {
		await this.openBulkWizardAtOwners(reportName);
		await this.pickOwners(owners);
		await this.createPicked(owners.length, owners.length, 0);
		await this.dialog.getByRole("button", { name: "Done" }).click();
		return this.waitUntilReady();
	}

	resultLine(text: string): Locator {
		return this.dialog.getByText(text);
	}

	/** Ticks the row's checkbox. */
	async select(reportName: string, owner: ReportOwnerName): Promise<void> {
		await this.rowOf(reportName, owner).getByRole("checkbox").check();
	}

	/** Waits until the page counts the picked rows and offers its actions on them. */
	async waitForSelected(count: number): Promise<void> {
		await this.page.getByText(`${count} selected`, { exact: true }).waitFor();
		await this.page
			.getByRole("button", { name: `Remove (${count})` })
			.waitFor();
		await this.page
			.getByRole("button", { name: `Export PDF (${count})` })
			.waitFor();
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
