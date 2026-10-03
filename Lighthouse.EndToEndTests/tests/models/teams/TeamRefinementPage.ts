import type { Locator, Page } from "@playwright/test";

/**
 * A Team's Refinement tab: the heading that counts the Work Items in refinement, and their rows in
 * backlog order. Rows are found by role and by the Work Item's id, never by a test id; the demo data's
 * Work Items carry no tracker link, so a row is not assumed to hold one.
 */
export class TeamRefinementPage {
	constructor(private readonly page: Page) {}

	get heading(): Locator {
		return this.page.getByRole("heading", {
			name: /^\d+ Work Items? in Refinement$/,
		});
	}

	// The first row of the table is its header, so the first Work Item is the second row.
	get firstWorkItemRow(): Locator {
		return this.page.getByRole("row").nth(1);
	}

	workItemRow(referenceId: string): Locator {
		return this.page.getByRole("row").filter({ hasText: referenceId });
	}
}
