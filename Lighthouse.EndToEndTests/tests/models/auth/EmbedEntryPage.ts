import type { Locator, Page } from "@playwright/test";
import { KeycloakLoginPage } from "./KeycloakLoginPage";

/**
 * The backend-served embed surfaces: `/embed/start` (ADR-137 hop 1, which ends on a static terminal
 * page in the orphaned tab) and `/embed/enter` (ADR-129 hop 3, where only the refusal renders its
 * own page — a success redirects into the SPA).
 */
export class EmbedEntryPage {
	static readonly START_PATH = "/embed/start";

	static readonly GRANT_HEADING = "You are signed in to Lighthouse";
	static readonly REFUSAL_HEADING = "Lighthouse has nothing to show you";

	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	/**
	 * What `router.open` does from the Jira page: a top-level navigation, unauthenticated, which the
	 * instance answers with an OIDC challenge against whatever provider it is already configured with.
	 */
	async openSignInTab(nonce: string): Promise<KeycloakLoginPage> {
		await this.page.goto(
			`${EmbedEntryPage.START_PATH}?nonce=${encodeURIComponent(nonce)}`,
		);

		return new KeycloakLoginPage(this.page);
	}

	get signInCompleteHeading(): Locator {
		return this.page.getByRole("heading", {
			name: EmbedEntryPage.GRANT_HEADING,
		});
	}

	get noAccessHeading(): Locator {
		return this.page.getByRole("heading", {
			name: EmbedEntryPage.REFUSAL_HEADING,
		});
	}

	get refusalHeading(): Locator {
		return this.page.getByRole("heading", {
			name: "This Lighthouse embed link is no longer valid",
		});
	}
}
