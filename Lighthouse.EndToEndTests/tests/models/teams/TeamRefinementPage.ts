import type { Locator, Page } from "@playwright/test";

/**
 * A Team's Refinement tab: the heading that counts the Work Items in refinement, and the grid's rows
 * in backlog order. Rows are found by role and by the Work Item's id, never by a test id; the demo data's
 * Work Items carry no tracker link, so a row is not assumed to hold one.
 */
export class TeamRefinementPage {
	constructor(private readonly page: Page) {}

	get heading(): Locator {
		return this.page.getByRole("heading", {
			name: /^\d+ Work Items? in Refinement$/,
		});
	}

	get firstWorkItemRow(): Locator {
		return this.workItemRows.first();
	}

	workItemRow(referenceId: string): Locator {
		return this.workItemRows.filter({ hasText: referenceId });
	}

	// The grid's header is a row too, but it holds column headers rather than cells.
	private get workItemRows(): Locator {
		return this.page
			.getByRole("grid")
			.getByRole("row")
			.filter({ has: this.page.getByRole("gridcell") });
	}
}
