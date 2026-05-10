import type { Locator, Page } from "@playwright/test";

/**
 * Page Object Model for the System Settings → Access (System Admins) tab.
 *
 * Wraps interactions with the RBAC administration UI surface, including:
 * - Bootstrap banner and self-bootstrap action
 * - System Admins user table (grant / revoke / remove)
 * - SSO group mappings table (add / remove)
 * - RBAC Status diagnostic accordion
 *
 * Driving port: all actions ultimately invoke AuthorizationController endpoints
 * through the browser UI.
 */
export class RbacSettingsPage {
	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	// -------------------------------------------------------------------------
	// Navigation
	// -------------------------------------------------------------------------

	async goToAccessTab(): Promise<void> {
		// Click the "System Admins" tab to switch into the RBAC settings view.
		// We click rather than navigate via URL because client-side routing on the
		// same /settings pathname can be a no-op for page.goto.
		await this.page
			.getByRole("tab", { name: "System Admins" })
			.click();
		await this.rbacStatusIndicator.waitFor({ state: "visible" });
	}

	// -------------------------------------------------------------------------
	// Bootstrap
	// -------------------------------------------------------------------------

	get bootstrapButton(): Locator {
		return this.page.getByTestId("rbac-bootstrap-button");
	}

	get bootstrapBanner(): Locator {
		return this.page.getByText("No System Admin is assigned yet");
	}

	async becomeFirstSystemAdmin(): Promise<void> {
		await this.bootstrapButton.click();
	}

	// -------------------------------------------------------------------------
	// RBAC Status panel
	// -------------------------------------------------------------------------

	get rbacStatusIndicator(): Locator {
		return this.page.getByTestId("rbac-status-enabled");
	}

	// -------------------------------------------------------------------------
	// System Admins user table
	// -------------------------------------------------------------------------

	get usersTable(): Locator {
		return this.page.getByTestId("rbac-users-table");
	}

	getUserRow(userEmail: string): Locator {
		return this.usersTable.getByRole("row").filter({ hasText: userEmail });
	}

	async grantSystemAdmin(userProfileId: number): Promise<void> {
		await this.page
			.getByTestId(`rbac-grant-system-admin-${userProfileId}`)
			.click();
	}

	async revokeSystemAdmin(userProfileId: number): Promise<void> {
		await this.page
			.getByTestId(`rbac-revoke-system-admin-${userProfileId}`)
			.click();
	}

	async removeUser(userProfileId: number): Promise<void> {
		await this.page.getByTestId(`rbac-remove-user-${userProfileId}`).click();
		// Confirm the destructive-action dialog
		await this.page.getByRole("button", { name: "Confirm" }).click();
	}

	/** Returns the text of the System Admin status cell for a given user row. */
	async getSystemAdminStatus(userEmail: string): Promise<string> {
		const row = this.getUserRow(userEmail);
		// Column order: Display Name (0), Email (1), Subject (2), System Admin (3), Unassigned (4), Actions (5)
		const cell = row.getByRole("cell").nth(3);
		return (await cell.textContent()) ?? "";
	}

	// -------------------------------------------------------------------------
	// SSO Group Mappings table
	// -------------------------------------------------------------------------

	get groupMappingsTable(): Locator {
		return this.page.getByTestId("rbac-group-mappings-table");
	}

	get addGroupMappingButton(): Locator {
		return this.page.getByTestId("rbac-create-group-mapping");
	}

	async addSystemAdminGroupMapping(groupName: string): Promise<void> {
		// MUI TextField puts data-testid on the outer FormControl div; target the inner input.
		await this.page
			.getByTestId("rbac-group-mapping-group-value")
			.locator("input")
			.fill(groupName);
		await this.addGroupMappingButton.click();
	}

	async removeGroupMapping(mappingId: number): Promise<void> {
		await this.page.getByTestId(`rbac-remove-group-mapping-${mappingId}`).click();
	}

	getGroupMappingRow(groupName: string): Locator {
		return this.groupMappingsTable.getByRole("row").filter({ hasText: groupName });
	}
}
