import type { Locator, Page } from "@playwright/test";

/**
 * The usage data consent dialog, however it was opened.
 *
 * The same dialog serves the footer icon and the unprompted ask, deliberately - one dialog with one
 * set of copy rather than a second that drifts from the first - so this page object does not know
 * which of the two put it on screen.
 */
export class UsageDataDialogPage {
	constructor(private readonly page: Page) {}

	/**
	 * Clears this browser's record of having been shown the dialog, before anything loads.
	 *
	 * The shared fixture seeds that record so the modal does not swallow pointer events in every
	 * other spec. A spec that is about the dialog undoes it, and then takes the ordinary production
	 * path rather than a special one.
	 */
	static async asABrowserThatHasNeverBeenAsked(page: Page): Promise<void> {
		await page.addInitScript(() => {
			try {
				localStorage.removeItem("lighthouse:usagedata:asked");
				sessionStorage.removeItem("lighthouse:prompt-slot");
			} catch {
				// A browser that will not hold either is a browser that gets asked, which is the
				// state this wants anyway.
			}
		});
	}

	get dialog(): Locator {
		return this.page.getByRole("dialog", {
			name: "May Lighthouse send usage data?",
		});
	}

	get acceptButton(): Locator {
		return this.dialog.getByRole("button", { name: /^yes/i });
	}

	get declineButton(): Locator {
		return this.dialog.getByRole("button", { name: /^no/i });
	}

	async decline(): Promise<void> {
		await this.declineButton.click();
	}
}
