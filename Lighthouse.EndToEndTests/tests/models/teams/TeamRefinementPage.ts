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
			name: /^\d+ Work Items? in Refinement( · \d+ ready by votes)?$/,
		});
	}

	get firstWorkItemRow(): Locator {
		return this.workItemRows.first();
	}

	workItemRow(referenceId: string): Locator {
		return this.workItemRows.filter({ hasText: referenceId });
	}

	/** One of the three answers on a Work Item's row: "Yes", "Yes, if…" or "No". */
	answerButton(referenceId: string, answer: string): Locator {
		return this.workItemRow(referenceId).getByRole("button", {
			name: answer,
			exact: true,
		});
	}

	/** Votes on a Work Item, giving the name first when this browser has not voted before. */
	async vote(referenceId: string, answer: string, name: string): Promise<void> {
		await this.answerButton(referenceId, answer).click();

		const prompt = this.page.getByRole("dialog", { name: "Who is voting?" });
		await prompt.getByRole("textbox", { name: "Your name" }).fill(name);
		await prompt.getByRole("button", { name: "Vote" }).click();
		await prompt.waitFor({ state: "hidden" });
	}

	// The grid's header is a row too, but it holds column headers rather than cells.
	private get workItemRows(): Locator {
		return this.page
			.getByRole("grid")
			.getByRole("row")
			.filter({ has: this.page.getByRole("gridcell") });
	}
}
