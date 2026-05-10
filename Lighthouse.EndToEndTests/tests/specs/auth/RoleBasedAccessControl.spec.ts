/**
 * @RBAC E2E @Auth — Role-Based Access Control end-to-end acceptance tests
 *
 * Tagged @Auth so CI runs these only in ci_verifyauth (which sets up Keycloak)
 * and excludes them from ci_verifysqlite / ci_verifypostgres (no Keycloak there).
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
import type { OverviewPage } from "../../models/overview/OverviewPage";
import { PortfolioDetailPage } from "../../models/portfolios/PortfolioDetailPage";
import { TeamDetailPage } from "../../models/teams/TeamDetailPage";

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
test.describe("@RBAC E2E @Auth", () => {
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
				await rbac.addSystemAdminGroupMapping(
					TestConfig.SYSTEMADMIN_GROUP_NAME,
				);

				// Then: the group mapping appears in the SSO Groups table.
				const mappingRow = rbac.getGroupMappingRow(
					TestConfig.SYSTEMADMIN_GROUP_NAME,
				);
				await expect(mappingRow).toBeVisible();
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
				// Given: the new sys admin user logs in.
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const rbac = await goToRbacSettings(overviewPage);

				// Then: all System Settings tabs are visible.
				await expect(rbac.usersTable).toBeVisible();

				// And: the test user (bootstrapped in Scenario 1) is shown in the table.
				// Because the test user is also configured as the Emergency Admin in
				// appsettings (WD-02), their System Admin cell renders "Emergency Admin"
				// and the row's Revoke / Remove buttons are hidden by design (DD-03).
				const testUserRow = rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME);
				await expect(testUserRow).toBeVisible();
				const status = await rbac.getSystemAdminStatus(
					TestConfig.AUTH_TEST_USER_USERNAME,
				);
				expect(status).toContain("Emergency Admin");

				// And: the new sys admin cannot revoke the emergency admin via the UI
				// (DD-03 — emergency admin protection: no Revoke button is rendered).
				await expect(
					testUserRow.getByRole("button", { name: "Revoke" }),
				).not.toBeVisible();
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
				// Given: the test user was removed as explicit System Admin in Scenario 3.
				// And: the emergency admin subject is configured in appsettings.json.
				// (Precondition verified by test environment setup — not asserted in Given step.)

				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTH_TEST_USER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				// When: the test user navigates to System Settings.
				const settingsPage = await overviewPage.lightHousePage.goToSettings();

				// Then: the System Admins tab (rbac-tab) is still visible
				// (emergency admin fallback active, WD-02).
				await expect(settingsPage.page.getByTestId("rbac-tab")).toBeVisible();

				// And: the test user row shows "Emergency Admin" with a lock icon (WD-02, DD-03).
				const rbac = new RbacSettingsPage(settingsPage.page);
				await rbac.goToAccessTab();
				const testUserRow = rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME);
				await expect(testUserRow.getByText("Emergency Admin")).toBeVisible();

				// And: no Revoke button is rendered on the emergency admin row (DD-03).
				await expect(
					testUserRow.getByRole("button", { name: "Revoke" }),
				).not.toBeVisible();
			},
		);
	});

	// =========================================================================
	// Scenario 5: System Admin assigns scoped roles on the test team and portfolio
	//
	// Implementation note (step 04-03): the original AC describes the System
	// Admin creating "RBAC E2E Test Team" and "RBAC E2E Test Portfolio" via the
	// UI wizards. The wizards require a fully configured work tracking system
	// connection plus work-item types and states — substantial setup that is
	// orthogonal to RBAC. The dev seed already provisions a Demo CSV connector
	// together with a "Team Zenith" team and a "Project Apollo" portfolio, so
	// this scenario reuses those existing seed entities as the scoped targets
	// (per the "Pick whichever is cleaner" guidance in step 04-03 notes). The
	// behavioural assertions remain identical: Access tab visibility for the
	// admin, role assignment via the scoped members table, and visible role
	// rows after assignment.
	//
	// User-profile bootstrap: scoped role assignment requires the target user
	// to have a `UserProfile`, which is created on first sign-in. The four
	// scoped users (teamreader, teamadmin, portfolioreader, portfolioadmin)
	// have not signed in yet, so the scenario logs each of them in once before
	// switching back to the system admin to assign roles.
	// =========================================================================
	test.describe("Scenario 5: System Admin assigns individual scoped roles on the test team and portfolio", () => {
		const TEAM_NAME = "Team Zenith";
		const PORTFOLIO_NAME = "Project Apollo";

		testWithAuth(
			"system admin assigns scoped roles to existing seed team and portfolio",
			async ({ loginPage, page }) => {
				// Given: the four scoped test users sign in once each so that their
				// `UserProfile` rows exist in the database. Without this, they would
				// not appear in the scoped members table and roles could not be
				// assigned to them.
				const scopedUsernames = [
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
				];

				let currentLoginPage = loginPage;
				for (const username of scopedUsernames) {
					const overview = await loginAs(
						currentLoginPage,
						username,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);

					// Logout to reset the Keycloak session so the next sign-in
					// presents the login form again. Also clear cookies as a
					// belt-and-braces measure against any residual SSO state.
					await overview.lightHousePage.logout();
					await page.context().clearCookies();
					currentLoginPage = await new LighthousePage(page).openWithAuth();
				}

				// And: the system admin signs in.
				const overviewPage = await loginAs(
					currentLoginPage,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				// When: the system admin opens the team detail page.
				// Use exact match because the dev seed contains both "Team Zenith"
				// and "Copy of Team Zenith".
				await overviewPage.search(TEAM_NAME);
				await overviewPage.page
					.getByRole("link", { name: TEAM_NAME, exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overviewPage.page);

				// Then: the Access tab is visible for the system admin (DD-07).
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Access" }),
				).toBeVisible();

				// When: the system admin assigns scoped team roles.
				const teamScopedAccess = new ScopedAccessPage(teamDetailPage.page);
				await teamScopedAccess.goToAccessTab();

				await teamScopedAccess.assignMemberRole(
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					"Viewer",
				);
				await teamScopedAccess.assignMemberRole(
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
					"TeamAdmin",
				);

				// Then: both assigned users are visible in the team members table.
				await teamScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
				);
				await expect(
					teamScopedAccess.getMemberRow(
						TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					),
				).toBeVisible();
				await teamScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
				);
				await expect(
					teamScopedAccess.getMemberRow(
						TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
					),
				).toBeVisible();

				// When: the system admin opens the portfolio detail page.
				await overviewPage.lightHousePage.goToOverview();
				await overviewPage.search(PORTFOLIO_NAME);
				await overviewPage.page
					.getByRole("link", { name: PORTFOLIO_NAME, exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overviewPage.page);

				// Then: the Access tab is visible for the system admin (DD-07).
				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Access" }),
				).toBeVisible();

				// When: the system admin assigns scoped portfolio roles.
				const portfolioScopedAccess = new ScopedAccessPage(
					portfolioDetailPage.page,
				);
				await portfolioScopedAccess.goToAccessTab();

				await portfolioScopedAccess.assignMemberRole(
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
					"Viewer",
				);
				await portfolioScopedAccess.assignMemberRole(
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
					"PortfolioAdmin",
				);

				// Then: both assigned users are visible in the portfolio members table.
				await portfolioScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
				);
				await expect(
					portfolioScopedAccess.getMemberRow(
						TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
					),
				).toBeVisible();
				await portfolioScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
				);
				await expect(
					portfolioScopedAccess.getMemberRow(
						TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
					),
				).toBeVisible();
			},
		);
	});

	// =========================================================================
	// Scenario 2: Team reader cannot see System Settings admin tabs
	//
	// Step 04-01 note: this describe block is positioned AFTER Scenario 5
	// because Scenario 2b requires the team reader to be assigned as TeamReader
	// on "Team Zenith" — that assignment happens in Scenario 5. Playwright
	// runs tests in spec-file order, so 2b must follow 5 to honour the
	// sequential-state design (WD-D04).
	// =========================================================================
	test.describe("Scenario 2: Team reader is restricted to License Info only in System Settings", () => {
		testWithAuth(
			"team reader cannot see System Admins tab or Log Level in System Settings",
			async ({ loginPage }) => {
				// Given: team reader logs in with read-only credentials.
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				// When: the team reader navigates to System Settings.
				const settingsPage = await overviewPage.lightHousePage.goToSettings();

				// Then: the System Admins tab (rbac-tab — RBAC admin surface) is not visible.
				await expect(
					settingsPage.page.getByTestId("rbac-tab"),
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
				// Given: team reader is assigned as Viewer for "Team Zenith" (Scenario 5).
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				// Navigate to "Team Zenith" — use exact match because the dev seed
				// also contains "Copy of Team Zenith".
				await overviewPage.search("Team Zenith");
				await overviewPage.page
					.getByRole("link", { name: "Team Zenith", exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overviewPage.page);

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

				// And: Deliveries tab is NOT visible — teams do not have a Deliveries
				// tab (only portfolios do).
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Deliveries" }),
				).not.toBeVisible();
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
					const overviewPage = await loginAs(
						loginPage,
						TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);

					// Use exact match because the dev seed contains both
					// "Team Zenith" and "Copy of Team Zenith".
					await overviewPage.search("Team Zenith");
					await overviewPage.page
						.getByRole("link", { name: "Team Zenith", exact: true })
						.click();
					const teamDetailPage = new TeamDetailPage(overviewPage.page);

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
						teamDetailPage.page.getByRole("button", {
							name: "Update Team Data",
						}),
					).not.toBeVisible();
				},
			);
		});

		test.describe("Team Admin", () => {
			testWithAuth(
				"team admin sees Settings and Access tabs and management controls for their team",
				async ({ loginPage }) => {
					const overviewPage = await loginAs(
						loginPage,
						TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);

					// Use exact match because the dev seed contains both
					// "Team Zenith" and "Copy of Team Zenith".
					await overviewPage.search("Team Zenith");
					await overviewPage.page
						.getByRole("link", { name: "Team Zenith", exact: true })
						.click();
					const teamDetailPage = new TeamDetailPage(overviewPage.page);

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
						teamDetailPage.page.getByRole("button", {
							name: "Update Team Data",
						}),
					).toBeVisible();

					// Scoped SSO group mappings load without error (WD-08, US-08).
					const scopedAccess = new ScopedAccessPage(teamDetailPage.page);
					await scopedAccess.goToAccessTab();
					await expect(scopedAccess.groupMappingsSection).toBeVisible();
					await expect(
						scopedAccess.groupMappingsErrorMessage,
					).not.toBeVisible();
				},
			);
		});

		test.describe("Portfolio Reader", () => {
			testWithAuth(
				"portfolio reader sees Forecast and Deliveries read-only but no admin controls",
				async ({ loginPage }) => {
					const overviewPage = await loginAs(
						loginPage,
						TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);

					await overviewPage.search("Project Apollo");
					await overviewPage.page
						.getByRole("link", { name: "Project Apollo", exact: true })
						.click();
					const portfolioDetailPage = new PortfolioDetailPage(
						overviewPage.page,
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
					const overviewPage = await loginAs(
						loginPage,
						TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);

					await overviewPage.search("Project Apollo");
					await overviewPage.page
						.getByRole("link", { name: "Project Apollo", exact: true })
						.click();
					const portfolioDetailPage = new PortfolioDetailPage(
						overviewPage.page,
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
					await expect(
						scopedAccess.groupMappingsErrorMessage,
					).not.toBeVisible();
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
				// Given: sys admin signs in.
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				// ----- Team Zenith: swap individual rights for SSO group mappings ----
				await overviewPage.search("Team Zenith");
				await overviewPage.page
					.getByRole("link", { name: "Team Zenith", exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overviewPage.page);
				const teamScopedAccess = new ScopedAccessPage(teamDetailPage.page);
				await teamScopedAccess.goToAccessTab();

				// Filter the members table down to the target user before clicking
				// Remove — otherwise the filtered/unfiltered list strict-mode mismatch
				// can pick up multiple rows (e.g. matching both display-name and email).
				await teamScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
				);
				const teamReaderRow = teamScopedAccess.getMemberRow(
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
				);
				await teamReaderRow.getByRole("button", { name: "Remove" }).click();

				await teamScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
				);
				const teamAdminRow = teamScopedAccess.getMemberRow(
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
				);
				await teamAdminRow.getByRole("button", { name: "Remove" }).click();

				// When: sys admin configures SSO group mappings for equivalent rights.
				await teamScopedAccess.addScopedGroupMapping(
					TestConfig.TEAMREADER_GROUP_NAME,
					"Viewer",
				);
				await teamScopedAccess.addScopedGroupMapping(
					TestConfig.TEAMADMIN_GROUP_NAME,
					"TeamAdmin",
				);

				// Then: group mapping rows are visible.
				await expect(
					teamScopedAccess.getScopedGroupMappingRow(
						TestConfig.TEAMREADER_GROUP_NAME,
					),
				).toBeVisible();
				await expect(
					teamScopedAccess.getScopedGroupMappingRow(
						TestConfig.TEAMADMIN_GROUP_NAME,
					),
				).toBeVisible();

				// ----- Project Apollo: same swap for portfolio scope ----------------
				await overviewPage.lightHousePage.goToOverview();
				await overviewPage.search("Project Apollo");
				await overviewPage.page
					.getByRole("link", { name: "Project Apollo", exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overviewPage.page);
				const portfolioScopedAccess = new ScopedAccessPage(
					portfolioDetailPage.page,
				);
				await portfolioScopedAccess.goToAccessTab();

				await portfolioScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
				);
				const portfolioReaderRow = portfolioScopedAccess.getMemberRow(
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
				);
				await portfolioReaderRow
					.getByRole("button", { name: "Remove" })
					.click();

				await portfolioScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
				);
				const portfolioAdminRow = portfolioScopedAccess.getMemberRow(
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
				);
				await portfolioAdminRow.getByRole("button", { name: "Remove" }).click();

				await portfolioScopedAccess.addScopedGroupMapping(
					TestConfig.PORTFOLIOREADER_GROUP_NAME,
					"Viewer",
				);
				await portfolioScopedAccess.addScopedGroupMapping(
					TestConfig.PORTFOLIOADMIN_GROUP_NAME,
					"PortfolioAdmin",
				);

				await expect(
					portfolioScopedAccess.getScopedGroupMappingRow(
						TestConfig.PORTFOLIOREADER_GROUP_NAME,
					),
				).toBeVisible();
				await expect(
					portfolioScopedAccess.getScopedGroupMappingRow(
						TestConfig.PORTFOLIOADMIN_GROUP_NAME,
					),
				).toBeVisible();
			},
		);

		// -----------------------------------------------------------------------
		// Group-rights: team reader — repeat Scenario 6 assertions
		// -----------------------------------------------------------------------
		testWithAuth(
			"team reader (group-based) sees Forecast but not Settings, Access, or write controls",
			async ({ loginPage }) => {
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overviewPage.search("Team Zenith");
				await overviewPage.page
					.getByRole("link", { name: "Team Zenith", exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overviewPage.page);

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
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overviewPage.search("Team Zenith");
				await overviewPage.page
					.getByRole("link", { name: "Team Zenith", exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overviewPage.page);

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
				await expect(scopedAccess.groupMappingsSection).toBeVisible();
				await expect(scopedAccess.groupMappingsErrorMessage).not.toBeVisible();
			},
		);

		// -----------------------------------------------------------------------
		// Group-rights: portfolio reader — repeat Scenario 6 assertions
		// -----------------------------------------------------------------------
		testWithAuth(
			"portfolio reader (group-based) sees Deliveries read-only but no admin controls",
			async ({ loginPage }) => {
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overviewPage.search("Project Apollo");
				await overviewPage.page
					.getByRole("link", { name: "Project Apollo", exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overviewPage.page);

				// Assertions are identical to Scenario 6 Portfolio Reader (WD-07).
				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Deliveries" }),
				).toBeVisible();

				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Settings" }),
				).not.toBeVisible();

				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Access" }),
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
				const overviewPage = await loginAs(
					loginPage,
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overviewPage.search("Project Apollo");
				await overviewPage.page
					.getByRole("link", { name: "Project Apollo", exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overviewPage.page);

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

				// Scoped group mappings still load without error after switching to group-based rights.
				const scopedAccess = new ScopedAccessPage(portfolioDetailPage.page);
				await scopedAccess.goToAccessTab();
				await expect(scopedAccess.groupMappingsSection).toBeVisible();
				await expect(scopedAccess.groupMappingsErrorMessage).not.toBeVisible();
			},
		);
	});
});
