import type { Locator, Page } from "@playwright/test";

/**
 * Page Object Model for the "Access" tab inside a Team or Portfolio detail page.
 *
 * Wraps interactions with:
 * - User member assignments (add / remove individual users)
 * - Scoped SSO group mappings (via the scoped endpoints)
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

	async assignMember(userEmail: string, role: "Viewer" | "Admin"): Promise<void> {
		await this.page.getByTestId("scoped-add-member-button").click();
		await this.page.getByTestId("scoped-member-email-input").fill(userEmail);
		await this.page.getByLabel("Role").click();
		await this.page.getByRole("option", { name: role }).click();
		await this.page.getByRole("button", { name: "Save" }).click();
	}

	getMemberRow(userEmail: string): Locator {
		return this.membersTable.getByRole("row").filter({ hasText: userEmail });
	}

	// -------------------------------------------------------------------------
	// Scoped SSO group mappings
	// -------------------------------------------------------------------------

	get groupMappingsSection(): Locator {
		return this.page.getByTestId("scoped-group-mappings-section");
	}

	/** Verifies the scoped group mappings load without a "Failed to load" error. */
	get groupMappingsErrorMessage(): Locator {
		return this.page.getByText("Failed to load");
	}

	async addScopedGroupMapping(groupName: string, role: "Viewer" | "Admin"): Promise<void> {
		await this.page.getByTestId("scoped-add-group-mapping-button").click();
		await this.page
			.getByTestId("rbac-group-mapping-group-value")
			.fill(groupName);
		await this.page.getByLabel("Role").click();
		await this.page.getByRole("option", { name: role }).click();
		await this.page.getByRole("button", { name: "Add" }).click();
	}

	getScopedGroupMappingRow(groupName: string): Locator {
		return this.groupMappingsSection
			.getByRole("row")
			.filter({ hasText: groupName });
	}
}
