import type { Locator, Page } from "@playwright/test";

/**
 * Scoped role label as it appears in the UI.
 *
 * Matches the `ScopedRbacRole` type on the backend / frontend (Viewer is the
 * scoped read-only role; TeamAdmin and PortfolioAdmin are the scoped admin
 * roles).
 */
export type ScopedRoleLabel = "Viewer" | "TeamAdmin" | "PortfolioAdmin";

/**
 * Page Object Model for the "Access" tab inside a Team or Portfolio detail page.
 *
 * Wraps interactions with:
 * - User member assignments (assign / remove individual users by user-profile id)
 * - Scoped SSO group mappings (via the scoped endpoints)
 *
 * Implementation note: the scoped members table is populated from existing
 * `UserProfiles` (created on first sign-in). To assign a role to a user, that
 * user must have a `UserProfile`. The UI renders one row per user-profile and
 * exposes per-row role buttons (`assign-{role}-{userProfileId}`).
 *
 * Driving port: GET/PUT/DELETE /authorization/teams/{id}/members,
 *               GET /authorization/teams/{id}/group-mappings (and portfolio equivalents)
 */
export class ScopedAccessPage {
	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	// -------------------------------------------------------------------------
	// Navigation
	// -------------------------------------------------------------------------

	async goToAccessTab(): Promise<void> {
		await this.page.getByRole("tab", { name: "Access" }).click();
	}

	// -------------------------------------------------------------------------
	// Member assignments
	// -------------------------------------------------------------------------

	get membersTable(): Locator {
		return this.page.getByTestId("scoped-members-table");
	}

	get membersSearch(): Locator {
		// MUI TextField applies the data-testid to the outer wrapper, so target
		// the inner <input> element to make `.fill()` work.
		return this.page.getByTestId("scoped-members-search").locator("input");
	}

	/**
	 * Assigns a scoped role to an existing user (identified by email).
	 *
	 * The user must already have a `UserProfile` (i.e. logged in at least once);
	 * otherwise the row will not be visible in the members table.
	 */
	async assignMemberRole(
		userEmail: string,
		role: ScopedRoleLabel,
	): Promise<void> {
		// Wait until the members fetch completes: either the loading indicator
		// goes away or, more reliably, the table renders at least one row
		// matching the target user. We rely on the table being populated as the
		// signal because the loading indicator briefly flickers on initial mount.
		await this.page
			.getByTestId("scoped-members-loading")
			.waitFor({ state: "detached" })
			.catch(() => {
				// Loading indicator may never have been visible — that's fine.
			});
		await this.membersTable.waitFor({ state: "visible" });
		await this.membersSearch.fill(userEmail);
		const memberRow = this.getMemberRow(userEmail);
		await memberRow.waitFor({ state: "visible" });
		await memberRow
			.getByRole("button", { name: this.roleButtonLabel(role) })
			.click();
	}

	getMemberRow(userEmail: string): Locator {
		return this.membersTable.getByRole("row").filter({ hasText: userEmail });
	}

	private roleButtonLabel(role: ScopedRoleLabel): string {
		// The UI renders human-readable role names ("Team Admin", "Portfolio Admin")
		// but the underlying enum values are the camel-case forms.
		switch (role) {
			case "Viewer":
				return "Viewer";
			case "TeamAdmin":
				return "Team Admin";
			case "PortfolioAdmin":
				return "Portfolio Admin";
		}
	}

	// -------------------------------------------------------------------------
	// Scoped SSO group mappings
	// -------------------------------------------------------------------------

	get groupMappingsSection(): Locator {
		return this.page.getByTestId("scoped-groups-table");
	}

	/** Verifies the scoped group mappings load without a "Failed to load" error. */
	get groupMappingsErrorMessage(): Locator {
		return this.page.getByText("Failed to load");
	}

	async addScopedGroupMapping(
		groupName: string,
		role: ScopedRoleLabel,
	): Promise<void> {
		await this.page.getByTestId("scoped-group-value-input").fill(groupName);
		await this.page.getByTestId("scoped-group-role-input").click();
		await this.page
			.getByRole("option", { name: this.roleButtonLabel(role) })
			.click();
		await this.page.getByTestId("scoped-group-add-button").click();
	}

	getScopedGroupMappingRow(groupName: string): Locator {
		return this.groupMappingsSection
			.getByRole("row")
			.filter({ hasText: groupName });
	}
}
