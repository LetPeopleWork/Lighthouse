import type { Locator, Page } from "@playwright/test";

/**
 * The `/embed/enter` entry point (ADR-129). It is served by the backend rather
 * than the SPA, so on success there is nothing to assert here — the browser is
 * redirected into the app. Only the refusal renders its own page.
 */
export class EmbedEntryPage {
	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get refusalHeading(): Locator {
		return this.page.getByRole("heading", {
			name: "This Lighthouse embed link is no longer valid",
		});
	}
}
