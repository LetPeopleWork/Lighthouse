/**
 * @RBAC E2E — Role-Based Access Control end-to-end acceptance tests
 *
 * Walking Skeleton Strategy: C (Real local)
 * All adapters are real: Playwright browser, Keycloak OIDC, .NET API, SQLite DB.
 *
 * Execution order matters. Scenarios are deliberately sequential because each
 * builds on state established by the previous one:
 *   1 → bootstrap first admin
 *   2 → verify viewer restriction (state from 1 active)
 *   3 → new sys admin manages rights, removes test user
 *   4 → emergency admin fallback still active for test user
 *   5 → sys admin creates team/portfolio and assigns scoped roles
 *   6 → each scoped user sees exactly the right view
 *   7 → group-based rights produce identical behaviour to individual rights
 *
 * Driving ports exercised through the browser:
 *   POST   /authorization/bootstrap/system-admin
 *   GET    /authorization/my-summary
 *   GET    /authorization/users
 *   POST   /authorization/system-admins/{id}
 *   DELETE /authorization/system-admins/{id}
 *   DELETE /authorization/users/{id}
 *   PUT    /authorization/teams/{id}/members/{userId}
 *   GET    /authorization/teams/{id}/group-mappings   (new scoped endpoint — WD-08)
 *   PUT    /authorization/portfolios/{id}/members/{userId}
 *   GET    /authorization/portfolios/{id}/group-mappings  (new scoped endpoint — DD-11)
 *   POST/DELETE /authorization/group-mappings/{mappingId?}
 */

import test from "@playwright/test";
import { TestConfig } from "../../../playwright.config";
import { expect, testWithAuth } from "../../fixutres/LighthouseFixture";
import { LighthousePage } from "../../models/app/LighthousePage";
import type { LoginPage } from "../../models/auth/LoginPage";
import { RbacSettingsPage } from "../../models/auth/rbac/RbacSettingsPage";
import { ScopedAccessPage } from "../../models/auth/rbac/ScopedAccessPage";
import { OverviewPage } from "../../models/overview/OverviewPage";

// ---------------------------------------------------------------------------
// Shared helper: login a user and navigate to the Overview
// ---------------------------------------------------------------------------
async function loginAs(
	loginPage: LoginPage,
	username: string,
	password: string,
): Promise<OverviewPage> {
	const keycloakPage = await loginPage.clickSignIn();
	return keycloakPage.login(username, password);
}

// ---------------------------------------------------------------------------
// Shared helper: navigate to System Settings → Access tab
// ---------------------------------------------------------------------------
async function goToRbacSettings(
	overviewPage: OverviewPage,
): Promise<RbacSettingsPage> {
	const settingsPage = await overviewPage.lightHousePage.goToSettings();
	const rbac = new RbacSettingsPage(settingsPage.page);
	await rbac.goToAccessTab();
	return rbac;
}

// ===========================================================================
// @walking-skeleton  Scenario 1: First user self-bootstraps as System Admin
// ===========================================================================
test.describe("@RBAC E2E", () => {
	test.describe("@walking-skeleton Scenario 1: Bootstrap first System Admin and assign SSO group", () => {
		testWithAuth(
			"first user self-bootstraps as System Admin and assigns SSO group",
			async ({ loginPage }) => {
				// Given: Lighthouse has OIDC enabled and no System Admin has been assigned yet.
				// (Asserted implicitly: the bootstrap button must be visible.)
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTH_TEST_USER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const rbac = await goToRbacSettings(overviewPage);

				// Then: the bootstrap banner is shown, indicating no System Admin exists.
				await expect(rbac.bootstrapBanner).toBeVisible();
				await expect(rbac.bootstrapButton).toBeVisible();

				// When: the test user clicks "Become First System Admin"
				await rbac.becomeFirstSystemAdmin();

				// Then: the test user appears in the System Admins table.
				await expect(rbac.usersTable).toBeVisible();
				const testUserRow = rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME);
				await expect(testUserRow).toBeVisible();

				// And: the bootstrap banner is no longer visible.
				await expect(rbac.bootstrapBanner).not.toBeVisible();

				// And: the status indicator confirms RBAC is now enabled.
				await expect(rbac.rbacStatusIndicator).toBeVisible();

				// When: the test user adds the system-admin SSO group mapping.
				await rbac.addSystemAdminGroupMapping(TestConfig.SYSTEMADMIN_GROUP_NAME);

				// Then: the group mapping appears in the SSO Groups table.
				const mappingRow = rbac.getGroupMappingRow(
					TestConfig.SYSTEMADMIN_GROUP_NAME,
				);
				await expect(mappingRow).toBeVisible();
			},
		);
	});

	// =========================================================================
	// Scenario 2: Team reader cannot see System Settings admin tabs
	// =========================================================================
	test.describe("Scenario 2: Team reader is restricted to License Info only in System Settings", () => {
		testWithAuth(
			"team reader cannot see System Admins tab or Log Level in System Settings",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 2 — depends on Scenario 1 having bootstrapped a System Admin. Enable after Scenario 1 is green.",
				);

				// Given: team reader logs in with read-only credentials.
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				// When: the team reader navigates to System Settings.
				const settingsPage =
					await overviewPage.lightHousePage.goToSettings();

				// Then: the System Admins tab (RBAC admin surface) is not visible.
				await expect(
					settingsPage.page.getByTestId("system-admins-tab"),
				).not.toBeVisible();

				// And: the Log Level section is not visible (System Admin only, WD-15).
				await expect(
					settingsPage.page.getByTestId("log-level-section"),
				).not.toBeVisible();

				// And: License Info is visible in read-only mode (WD-08).
				await expect(
					settingsPage.page.getByTestId("system-info-tab"),
				).toBeVisible();
			},
		);

		testWithAuth(
			"team reader cannot access team Settings or Access tabs",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 2 — depends on Scenario 5 having assigned the team reader. Enable after Scenario 5 is green.",
				);

				// Given: team reader is assigned as Viewer for the test team.
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				// (Navigate to the test team — ID determined from URL after creation in Scenario 5.)
				const teamDetailPage = await overviewPage.goToTeam(
					"RBAC E2E Test Team",
				);

				// Then: the Settings tab is NOT visible (WD-06, DD-07).
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Settings" }),
				).not.toBeVisible();

				// And: the Access tab is NOT visible (RBAC enabled AND only shown to admins).
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Access" }),
				).not.toBeVisible();

				// And: write controls are HIDDEN — not disabled (WD-06).
				await expect(
					teamDetailPage.page.getByRole("button", { name: "Update Team Data" }),
				).not.toBeVisible();

				// And: Deliveries tab IS visible in read-only mode (WD-12, DD-08).
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Deliveries" }),
				).not.toBeVisible(); // teams may not have a Deliveries tab; portfolios do
			},
		);
	});

	// =========================================================================
	// Scenario 3: New System Admin manages rights and removes test user
	// =========================================================================
	test.describe("Scenario 3: New System Admin sees all tabs, revokes test user's admin role", () => {
		testWithAuth(
			"new sys admin can access all System Settings tabs and revoke test user rights",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 3 — depends on Scenario 1. Enable after Scenario 1 is green.",
				);

				// Given: the new sys admin user logs in.
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const rbac = await goToRbacSettings(overviewPage);

				// Then: all System Settings tabs are visible.
				await expect(rbac.usersTable).toBeVisible();

				// And: the test user (bootstrapped in Scenario 1) is shown as System Admin.
				const testUserRow = rbac.getUserRow(
					TestConfig.AUTH_TEST_USER_USERNAME,
				);
				await expect(testUserRow).toBeVisible();
				const status = await rbac.getSystemAdminStatus(
					TestConfig.AUTH_TEST_USER_USERNAME,
				);
				expect(status).toContain("Yes");

				// When: the new sys admin revokes the test user's System Admin role.
				// (Requires the user profile ID; resolved at runtime from the row.)
				// For now we use a named approach via the row-level revoke button.
				const revokeButton = testUserRow.getByRole("button", {
					name: "Revoke",
				});
				await revokeButton.click();

				// Then: the test user's row shows "No" for System Admin.
				await expect(
					testUserRow.getByText("No"),
				).toBeVisible();
			},
		);
	});

	// =========================================================================
	// Scenario 4: Emergency admin fallback — test user retains full access
	// =========================================================================
	test.describe("Scenario 4: Emergency admin fallback remains active after explicit revocation", () => {
		testWithAuth(
			"test user (emergency admin) still sees full admin access after being removed as explicit System Admin",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 4 — depends on Scenario 3. Enable after Scenario 3 is green.",
				);

				// Given: the test user was removed as explicit System Admin in Scenario 3.
				// And: the emergency admin subject is configured in appsettings.json.
				// (Precondition verified by test environment setup — not asserted in Given step.)

				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTH_TEST_USER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				// When: the test user navigates to System Settings.
				const settingsPage =
					await overviewPage.lightHousePage.goToSettings();

				// Then: the System Admins tab is still visible (emergency admin fallback active, WD-02).
				await expect(
					settingsPage.page.getByTestId("system-admins-tab"),
				).toBeVisible();

				// And: the test user row shows "Emergency Admin" with a lock icon (WD-02, DD-03).
				const rbac = new RbacSettingsPage(settingsPage.page);
				await rbac.goToAccessTab();
				const testUserRow = rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME);
				await expect(
					testUserRow.getByText("Emergency Admin"),
				).toBeVisible();

				// And: no Revoke button is rendered on the emergency admin row (DD-03).
				await expect(
					testUserRow.getByRole("button", { name: "Revoke" }),
				).not.toBeVisible();
			},
		);
	});

	// =========================================================================
	// Scenario 5: System Admin creates team + portfolio, assigns scoped roles
	// =========================================================================
	test.describe("Scenario 5: System Admin creates test team and portfolio, assigns individual scoped roles", () => {
		testWithAuth(
			"system admin creates test entities and assigns individual user roles",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 5 — depends on Scenario 1. Enable after Scenario 1 is green.",
				);

				// Given: the system admin user is logged in.
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				// When: the system admin creates "RBAC E2E Test Team".
				const addTeamWizard = await overviewPage.lightHousePage.createNewTeam();
				// (Team creation wizard steps — implementation depends on wizard model)
				// Minimal: set name and save; data-source details not required for RBAC gating tests.
				await addTeamWizard.page.getByLabel("Name").fill("RBAC E2E Test Team");
				await addTeamWizard.page.getByRole("button", { name: "Save" }).click();

				// Navigate back to overview to get team link.
				await overviewPage.lightHousePage.goToOverview();
				const teamDetailPage = await overviewPage.goToTeam("RBAC E2E Test Team");

				// Then: the team was created and Access tab is present for the sys admin.
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Access" }),
				).toBeVisible();

				// When: the system admin opens the Access tab and assigns scoped roles.
				const scopedAccess = new ScopedAccessPage(teamDetailPage.page);
				await scopedAccess.goToAccessTab();

				// Assign team reader (Viewer) and team admin (Admin).
				await scopedAccess.assignMember(
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					"Viewer",
				);
				await scopedAccess.assignMember(
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
					"Admin",
				);

				// Then: both assignments are visible in the members table.
				await expect(
					scopedAccess.getMemberRow(TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME),
				).toBeVisible();
				await expect(
					scopedAccess.getMemberRow(TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME),
				).toBeVisible();

				// (Portfolio creation and assignment omitted for now — symmetric with team.)
				// TODO: create "RBAC E2E Test Portfolio" and assign portfolio reader + admin.
			},
		);
	});

	// =========================================================================
	// Scenario 6: Per-user access verification — individual rights
	// =========================================================================
	test.describe("Scenario 6: Each scoped user sees exactly the right view (individual rights)", () => {
		test.describe("Team Reader", () => {
			testWithAuth(
				"team reader sees Forecast tab but not Settings, Access, or write controls on their team",
				async ({ loginPage }) => {
					test.skip(
						true,
						"Scenario 6 / Team Reader — depends on Scenario 5. Enable after Scenario 5 is green.",
					);

					const overviewPage = await loginAs(
						loginPage,
						TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);

					const teamDetailPage = await overviewPage.goToTeam(
						"RBAC E2E Test Team",
					);

					// Forecast tab is visible and reachable.
					await expect(
						teamDetailPage.page.getByRole("tab", { name: "Forecast" }),
					).toBeVisible();

					// Settings tab is NOT visible (WD-06, DD-07).
					await expect(
						teamDetailPage.page.getByRole("tab", { name: "Settings" }),
					).not.toBeVisible();

					// Access tab is NOT visible (no admin role, DD-07).
					await expect(
						teamDetailPage.page.getByRole("tab", { name: "Access" }),
					).not.toBeVisible();

					// Update Team Data write control is HIDDEN — not disabled (WD-06, DD-01).
					await expect(
						teamDetailPage.page.getByRole("button", { name: "Update Team Data" }),
					).not.toBeVisible();

					// Clone control is HIDDEN (US-07, DD-01).
					await expect(
						teamDetailPage.page.getByRole("button", { name: "Clone" }),
					).not.toBeVisible();

					// Delete control is HIDDEN (US-07, DD-01).
					await expect(
						teamDetailPage.page.getByRole("button", { name: "Delete" }),
					).not.toBeVisible();
				},
			);
		});

		test.describe("Team Admin", () => {
			testWithAuth(
				"team admin sees Settings and Access tabs and management controls for their team",
				async ({ loginPage }) => {
					test.skip(
						true,
						"Scenario 6 / Team Admin — depends on Scenario 5. Enable after Scenario 5 is green.",
					);

					const overviewPage = await loginAs(
						loginPage,
						TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);

					const teamDetailPage = await overviewPage.goToTeam(
						"RBAC E2E Test Team",
					);

					// Settings tab IS visible for Team Admin (DD-10, US-06).
					await expect(
						teamDetailPage.page.getByRole("tab", { name: "Settings" }),
					).toBeVisible();

					// Access tab IS visible when RBAC enabled and Team Admin (DD-07, US-06).
					await expect(
						teamDetailPage.page.getByRole("tab", { name: "Access" }),
					).toBeVisible();

					// Management write controls are visible (US-07).
					await expect(
						teamDetailPage.page.getByRole("button", { name: "Update Team Data" }),
					).toBeVisible();

					// Scoped SSO group mappings load without error (WD-08, US-08).
					const scopedAccess = new ScopedAccessPage(teamDetailPage.page);
					await scopedAccess.goToAccessTab();
					await expect(scopedAccess.groupMappingsSection).toBeVisible();
					await expect(scopedAccess.groupMappingsErrorMessage).not.toBeVisible();
				},
			);
		});

		test.describe("Portfolio Reader", () => {
			testWithAuth(
				"portfolio reader sees Forecast and Deliveries read-only but no admin controls",
				async ({ loginPage }) => {
					test.skip(
						true,
						"Scenario 6 / Portfolio Reader — depends on Scenario 5 portfolio creation. Enable after Scenario 5 is green.",
					);

					const overviewPage = await loginAs(
						loginPage,
						TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);

					const portfolioDetailPage = await overviewPage.goToPortfolio(
						"RBAC E2E Test Portfolio",
					);

					// Deliveries tab IS visible (WD-12, DD-08).
					await expect(
						portfolioDetailPage.page.getByRole("tab", { name: "Deliveries" }),
					).toBeVisible();

					// Settings tab is NOT visible (WD-06, DD-07 for portfolio reader).
					await expect(
						portfolioDetailPage.page.getByRole("tab", { name: "Settings" }),
					).not.toBeVisible();

					// Access tab is NOT visible.
					await expect(
						portfolioDetailPage.page.getByRole("tab", { name: "Access" }),
					).not.toBeVisible();

					// Navigate to Deliveries tab — Add Delivery button is NOT visible (DD-08).
					const deliveriesPage = await portfolioDetailPage.goToDeliveries();
					await expect(
						deliveriesPage.page.getByRole("button", { name: "Add Delivery" }),
					).not.toBeVisible();
				},
			);
		});

		test.describe("Portfolio Admin", () => {
			testWithAuth(
				"portfolio admin sees Settings and Access tabs and can manage deliveries",
				async ({ loginPage }) => {
					test.skip(
						true,
						"Scenario 6 / Portfolio Admin — depends on Scenario 5. Enable after Scenario 5 is green.",
					);

					const overviewPage = await loginAs(
						loginPage,
						TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);

					const portfolioDetailPage = await overviewPage.goToPortfolio(
						"RBAC E2E Test Portfolio",
					);

					// Settings tab IS visible (DD-10, US-06).
					await expect(
						portfolioDetailPage.page.getByRole("tab", { name: "Settings" }),
					).toBeVisible();

					// Access tab IS visible (DD-07, US-06).
					await expect(
						portfolioDetailPage.page.getByRole("tab", { name: "Access" }),
					).toBeVisible();

					// Deliveries tab is visible and Add Delivery IS available (DD-08).
					const deliveriesPage = await portfolioDetailPage.goToDeliveries();
					await expect(
						deliveriesPage.page.getByRole("button", { name: "Add Delivery" }),
					).toBeVisible();

					// Scoped SSO group mappings load without error (DD-11, US-08 portfolio scope).
					const scopedAccess = new ScopedAccessPage(portfolioDetailPage.page);
					await scopedAccess.goToAccessTab();
					await expect(scopedAccess.groupMappingsSection).toBeVisible();
					await expect(scopedAccess.groupMappingsErrorMessage).not.toBeVisible();
				},
			);
		});
	});

	// =========================================================================
	// Scenario 7: Group-based rights produce identical behaviour to Scenario 6
	// =========================================================================
	test.describe("Scenario 7: Group-based rights are behaviourally identical to individual rights", () => {
		testWithAuth(
			"system admin switches to group-based rights for all scoped roles",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 7 setup — depends on Scenario 6 completing. Enable after Scenario 6 is green.",
				);

				// Given: sys admin removes individual role assignments.
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const teamDetailPage = await overviewPage.goToTeam(
					"RBAC E2E Test Team",
				);
				const scopedAccess = new ScopedAccessPage(teamDetailPage.page);
				await scopedAccess.goToAccessTab();

				// Remove individual assignments.
				const teamReaderRow = scopedAccess.getMemberRow(
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
				);
				await teamReaderRow.getByRole("button", { name: "Remove" }).click();

				const teamAdminRow = scopedAccess.getMemberRow(
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
				);
				await teamAdminRow.getByRole("button", { name: "Remove" }).click();

				// When: sys admin configures SSO group mappings for equivalent rights.
				await scopedAccess.addScopedGroupMapping(
					TestConfig.TEAMREADER_GROUP_NAME,
					"Viewer",
				);
				await scopedAccess.addScopedGroupMapping(
					TestConfig.TEAMADMIN_GROUP_NAME,
					"Admin",
				);

				// Then: group mapping rows are visible.
				await expect(
					scopedAccess.getScopedGroupMappingRow(TestConfig.TEAMREADER_GROUP_NAME),
				).toBeVisible();
				await expect(
					scopedAccess.getScopedGroupMappingRow(TestConfig.TEAMADMIN_GROUP_NAME),
				).toBeVisible();
			},
		);

		// -----------------------------------------------------------------------
		// Group-rights: team reader — repeat Scenario 6 assertions
		// -----------------------------------------------------------------------
		testWithAuth(
			"team reader (group-based) sees Forecast but not Settings, Access, or write controls",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 7 / Group team reader — depends on Scenario 7 setup. Enable after setup test is green.",
				);

				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const teamDetailPage = await overviewPage.goToTeam(
					"RBAC E2E Test Team",
				);

				// Assertions are identical to Scenario 6 Team Reader (WD-07).
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Forecast" }),
				).toBeVisible();

				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Settings" }),
				).not.toBeVisible();

				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Access" }),
				).not.toBeVisible();

				await expect(
					teamDetailPage.page.getByRole("button", { name: "Update Team Data" }),
				).not.toBeVisible();
			},
		);

		// -----------------------------------------------------------------------
		// Group-rights: team admin — repeat Scenario 6 assertions
		// -----------------------------------------------------------------------
		testWithAuth(
			"team admin (group-based) sees Settings and Access tabs and management controls",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 7 / Group team admin — depends on Scenario 7 setup. Enable after setup test is green.",
				);

				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const teamDetailPage = await overviewPage.goToTeam(
					"RBAC E2E Test Team",
				);

				// Assertions are identical to Scenario 6 Team Admin (WD-07).
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Settings" }),
				).toBeVisible();

				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Access" }),
				).toBeVisible();

				await expect(
					teamDetailPage.page.getByRole("button", { name: "Update Team Data" }),
				).toBeVisible();

				// Scoped group mappings still load without error after switching to group-based rights.
				const scopedAccess = new ScopedAccessPage(teamDetailPage.page);
				await scopedAccess.goToAccessTab();
				await expect(scopedAccess.groupMappingsErrorMessage).not.toBeVisible();
			},
		);

		// -----------------------------------------------------------------------
		// Group-rights: portfolio reader — repeat Scenario 6 assertions
		// -----------------------------------------------------------------------
		testWithAuth(
			"portfolio reader (group-based) sees Deliveries read-only but no admin controls",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 7 / Group portfolio reader — depends on Scenario 7 portfolio group setup. Enable after group setup is green.",
				);

				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const portfolioDetailPage = await overviewPage.goToPortfolio(
					"RBAC E2E Test Portfolio",
				);

				// Assertions are identical to Scenario 6 Portfolio Reader (WD-07).
				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Deliveries" }),
				).toBeVisible();

				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Settings" }),
				).not.toBeVisible();

				const deliveriesPage = await portfolioDetailPage.goToDeliveries();
				await expect(
					deliveriesPage.page.getByRole("button", { name: "Add Delivery" }),
				).not.toBeVisible();
			},
		);

		// -----------------------------------------------------------------------
		// Group-rights: portfolio admin — repeat Scenario 6 assertions
		// -----------------------------------------------------------------------
		testWithAuth(
			"portfolio admin (group-based) sees Settings, Access, and can manage deliveries",
			async ({ loginPage }) => {
				test.skip(
					true,
					"Scenario 7 / Group portfolio admin — depends on Scenario 7 portfolio group setup. Enable after group setup is green.",
				);

				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const portfolioDetailPage = await overviewPage.goToPortfolio(
					"RBAC E2E Test Portfolio",
				);

				// Assertions are identical to Scenario 6 Portfolio Admin (WD-07).
				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Settings" }),
				).toBeVisible();

				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Access" }),
				).toBeVisible();

				const deliveriesPage = await portfolioDetailPage.goToDeliveries();
				await expect(
					deliveriesPage.page.getByRole("button", { name: "Add Delivery" }),
				).toBeVisible();
			},
		);
	});
});
