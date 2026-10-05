import type { Locator, Page } from "@playwright/test";

const NEXT_REFINEMENT =
	/^Next Refinement: \w{3} \d{1,2} \w{3} · (tomorrow|in \d+ days)$/;

/**
 * A Team's Refinement tab: the heading - the count of Work Items in refinement, or the stage breakdown
 * once the Team has stage rules - and the grid's rows in backlog order. Rows are found by role and by
 * the Work Item's id, never by a test id; the demo data's Work Items carry no tracker link, so a row is
 * not assumed to hold one.
 */
export class TeamRefinementPage {
	constructor(private readonly page: Page) {}

	get heading(): Locator {
		return this.page.getByRole("heading", {
			name: /^(\d+ Work Items? in Refinement · \d+ ready by votes|\d+ Ready · \d+ Being refined · \d+ Waiting)$/,
		});
	}

	/** The vote column's header, which asks the yardstick question. */
	get voteColumnHeader(): Locator {
		return this.page.getByRole("columnheader", {
			name: /^Doable within (\d+ days?|our .+)\?/,
		});
	}

	/** What the heading's row says for a Team without a Refinement cadence; the hint is the icon's tooltip. */
	get noCadence(): Locator {
		return this.page.getByText("No Refinement cadence", { exact: true });
	}

	/** The next Refinement: the day, the date and how far off it is. */
	get nextRefinement(): Locator {
		return this.page.getByText(NEXT_REFINEMENT);
	}

	/** Whether to refine more or stop, or why the tab cannot say, titled with the next Refinement. */
	get needMessage(): Locator {
		return this.page.getByRole("alert");
	}

	/** The next Refinement as the need message's title. */
	get needMessageTitle(): Locator {
		return this.needMessage.getByText(NEXT_REFINEMENT);
	}

	/** The verdict beneath the need message's title. */
	get verdict(): Locator {
		return this.needMessage.getByText(/^\d+ ready — /);
	}

	/** The line after the Work Items needed before the next Refinement. */
	get enoughForLine(): Locator {
		return this.page.getByText(
			/^(enough for the next Refinement \(\d+%\) · not needed before then|All \d+ Work Items in Refinement are needed before the next Refinement\.|The only Work Item in Refinement is needed before the next Refinement\.)$/,
		);
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
