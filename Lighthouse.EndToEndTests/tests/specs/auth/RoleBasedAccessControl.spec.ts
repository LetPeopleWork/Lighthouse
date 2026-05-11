import path from "node:path";
import test from "@playwright/test";
import { TestConfig } from "../../../playwright.config";
import { expect, testWithAuth } from "../../fixutres/LighthouseFixture";
import { LighthousePage } from "../../models/app/LighthousePage";
import { BlockedPage } from "../../models/auth/BlockedPage";
import type { LoginPage } from "../../models/auth/LoginPage";
import { RbacSettingsPage } from "../../models/auth/rbac/RbacSettingsPage";
import { ScopedAccessPage } from "../../models/auth/rbac/ScopedAccessPage";
import { OverviewPage } from "../../models/overview/OverviewPage";
import { PortfolioDetailPage } from "../../models/portfolios/PortfolioDetailPage";
import { TeamDetailPage } from "../../models/teams/TeamDetailPage";

const LICENSE_FILE_PATH = path.join(
	process.cwd(),
	"..",
	"Lighthouse.Backend",
	"Lighthouse.Backend.Tests",
	"Services",
	"Implementation",
	"Licensing",
	"valid_not_expired_license.json",
);

const TEAM_NAME = "Team Zenith";
const PORTFOLIO_NAME = "Project Apollo";

async function loginAs(
	loginPage: LoginPage,
	username: string,
	password: string,
): Promise<OverviewPage> {
	const keycloakPage = await loginPage.clickSignIn();
	return keycloakPage.login(username, password);
}

async function loginAsWithLicense(
	loginPage: LoginPage,
	username: string,
	password: string,
): Promise<OverviewPage> {
	const overviewPage = await loginAs(loginPage, username, password);

	const overviewLink = overviewPage.page.getByRole("link", {
		name: "Overview",
	});
	const blockedTitle = overviewPage.page.getByText("LighthousePremium License");

	await Promise.any([
		overviewLink.waitFor({ state: "visible" }),
		blockedTitle.waitFor({ state: "visible" }),
	]);

	if (await blockedTitle.isVisible()) {
		const blockedPage = new BlockedPage(overviewPage.page);
		return blockedPage.uploadLicense(LICENSE_FILE_PATH);
	}

	return new OverviewPage(
		overviewPage.page,
		new LighthousePage(overviewPage.page),
	);
}

async function switchUser(
	overview: OverviewPage,
	username: string,
	password: string,
): Promise<OverviewPage> {
	await overview.lightHousePage.logout();
	await overview.page.context().clearCookies();
	const lighthousePage = new LighthousePage(overview.page);
	const loginPage = await lighthousePage.openWithAuth();
	return loginAs(loginPage, username, password);
}

async function goToRbacSettings(
	overviewPage: OverviewPage,
): Promise<RbacSettingsPage> {
	const settingsPage = await overviewPage.lightHousePage.goToSettings();
	const rbac = new RbacSettingsPage(settingsPage.page);
	await rbac.goToAccessTab();
	return rbac;
}

test.describe("@rbac E2E", () => {
	testWithAuth(
		"end-to-end role-based access control across bootstrap, scoped roles, and group mappings",
		async ({ loginPage }) => {
			let overview: OverviewPage;

			await test.step("first user self-bootstraps as System Admin and assigns SSO group", async () => {
				overview = await loginAsWithLicense(
					loginPage,
					TestConfig.AUTH_TEST_USER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await expect(
					overview.page.getByTestId("update-all-button"),
				).toBeVisible();
				await expect(
					overview.page.getByTestId("onboarding-stepper"),
				).toBeVisible();
				await expect(
					overview.page.getByRole("button", { name: "Add Team" }).first(),
				).toBeVisible();
				await expect(
					overview.page.getByRole("button", { name: "Add Portfolio" }).first(),
				).toBeVisible();

				const rbac = await goToRbacSettings(overview);
				await expect(rbac.page.getByTestId("api-keys-tab")).toBeVisible();

				await overview.page.getByTestId("license-status-button").click();
				await expect(
					overview.page.getByTestId("license-add-button"),
				).toBeVisible();
				await expect(
					overview.page.getByTestId("license-clear-button"),
				).toBeVisible();
				await overview.page.keyboard.press("Escape");

				await expect(rbac.bootstrapBanner).toBeVisible();
				await expect(rbac.bootstrapButton).toBeVisible();

				await rbac.becomeFirstSystemAdmin();

				await expect(rbac.usersTable).toBeVisible();
				await expect(
					rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME),
				).toBeVisible();
				await expect(rbac.bootstrapBanner).not.toBeVisible();
				await expect(rbac.rbacStatusIndicator).toBeVisible();

				await rbac.addSystemAdminGroupMapping(
					TestConfig.SYSTEMADMIN_GROUP_NAME,
				);
				await expect(
					rbac.getGroupMappingRow(TestConfig.SYSTEMADMIN_GROUP_NAME),
				).toBeVisible();
			});

			await test.step(
				"load 'When Will This Be Done?' demo scenario so Team Zenith and Project Apollo exist for the scoped-role steps",
				async () => {
					// The rbac flow asserts against entities provisioned by demo scenario 0
					// (Team Zenith, Project Apollo). On a fresh CI/local DB these do not
					// exist, so load them via the demo API while the test user is the
					// System Admin from Scenario 1.
					const loadResponse = await overview.page.request.post(
						"/api/latest/demo/scenarios/0/load",
					);
					expect(loadResponse.ok()).toBe(true);
					await overview.lightHousePage.goToOverview();
					await overview.search(TEAM_NAME);
					await expect(
						overview.page.getByRole("link", { name: TEAM_NAME, exact: true }),
					).toBeVisible();
				},
			);

			const scopedUsernames = [
				TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
				TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
				TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
				TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
			];

			for (const username of scopedUsernames) {
				await test.step(`bootstrap UserProfile for ${username}`, async () => {
					overview = await switchUser(
						overview,
						username,
						TestConfig.AUTH_TEST_USER_PASSWORD,
					);
				});
			}

			await test.step("new System Admin (group-mapped) sees test user as Emergency Admin and cannot revoke", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const rbac = await goToRbacSettings(overview);
				await expect(rbac.usersTable).toBeVisible();

				const testUserRow = rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME);
				await expect(testUserRow).toBeVisible();

				const status = await rbac.getSystemAdminStatus(
					TestConfig.AUTH_TEST_USER_USERNAME,
				);
				expect(status).toContain("Emergency Admin");

				await expect(
					testUserRow.getByRole("button", { name: "Revoke" }),
				).not.toBeVisible();
			});

			await test.step("emergency admin fallback — test user retains admin access via configuration", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTH_TEST_USER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				const settingsPage = await overview.lightHousePage.goToSettings();
				await expect(settingsPage.page.getByTestId("rbac-tab")).toBeVisible();

				const rbac = new RbacSettingsPage(settingsPage.page);
				await rbac.goToAccessTab();
				const testUserRow = rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME);
				await expect(testUserRow.getByText("Emergency Admin")).toBeVisible();
				await expect(
					testUserRow.getByRole("button", { name: "Revoke" }),
				).not.toBeVisible();
			});

			await test.step("System Admin assigns individual scoped roles on seed team and portfolio", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overview.search(TEAM_NAME);
				const systemAdminTeamRow = overview.page
					.getByRole("row")
					.filter({ hasText: TEAM_NAME });
				await expect(
					systemAdminTeamRow.getByRole("link", { name: "Edit" }),
				).toBeVisible();
				await expect(
					systemAdminTeamRow.getByRole("button", { name: "Clone" }),
				).toBeVisible();
				await expect(
					systemAdminTeamRow.getByRole("button", { name: "Delete" }),
				).toBeVisible();
				await overview.page
					.getByRole("link", { name: TEAM_NAME, exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overview.page);

				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Access" }),
				).toBeVisible();

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

				await overview.lightHousePage.goToOverview();
				await overview.search(PORTFOLIO_NAME);
				const systemAdminPortfolioRow = overview.page
					.getByRole("row")
					.filter({ hasText: PORTFOLIO_NAME });
				await expect(
					systemAdminPortfolioRow.getByRole("link", { name: "Edit" }),
				).toBeVisible();
				await expect(
					systemAdminPortfolioRow.getByRole("button", { name: "Clone" }),
				).toBeVisible();
				await expect(
					systemAdminPortfolioRow.getByRole("button", { name: "Delete" }),
				).toBeVisible();
				await overview.page
					.getByRole("link", { name: PORTFOLIO_NAME, exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overview.page);

				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Access" }),
				).toBeVisible();

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
			});

			await test.step("team reader is restricted to License Info in System Settings", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await expect(
					overview.page.getByTestId("update-all-button"),
				).not.toBeVisible();
				await expect(
					overview.page.getByTestId("onboarding-stepper"),
				).not.toBeVisible();
				await expect(
					overview.page.getByRole("button", { name: "Add Team" }),
				).not.toBeVisible();
				await expect(
					overview.page.getByRole("button", { name: "Add Portfolio" }),
				).not.toBeVisible();

				await overview.search(TEAM_NAME);
				const teamReaderTeamRow = overview.page
					.getByRole("row")
					.filter({ hasText: TEAM_NAME });
				await expect(
					teamReaderTeamRow.getByRole("link", { name: "Details" }),
				).toBeVisible();
				await expect(
					teamReaderTeamRow.getByRole("link", { name: "Edit" }),
				).not.toBeVisible();
				await expect(
					teamReaderTeamRow.getByRole("button", { name: "Clone" }),
				).not.toBeVisible();
				await expect(
					teamReaderTeamRow.getByRole("button", { name: "Delete" }),
				).not.toBeVisible();

				// Team Reader has no portfolio access — the Project Apollo row is
				// filtered out by the backend (GetReadablePortfolioIdsAsync), so the
				// entire row is absent rather than rendered with read-only icons.
				await overview.search(PORTFOLIO_NAME);
				await expect(
					overview.page
						.getByRole("row")
						.filter({ hasText: PORTFOLIO_NAME }),
				).toHaveCount(0);

				await overview.page.getByTestId("license-status-button").click();
				await expect(
					overview.page.getByTestId("license-add-button"),
				).not.toBeVisible();
				await expect(
					overview.page.getByTestId("license-clear-button"),
				).not.toBeVisible();
				await overview.page.keyboard.press("Escape");

				const settingsPage = await overview.lightHousePage.goToSettings();
				await expect(
					settingsPage.page.getByTestId("rbac-tab"),
				).not.toBeVisible();
				await expect(
					settingsPage.page.getByTestId("log-level-section"),
				).not.toBeVisible();
				await expect(
					settingsPage.page.getByTestId("api-keys-tab"),
				).not.toBeVisible();
				await expect(
					settingsPage.page.getByTestId("system-info-tab"),
				).toBeVisible();
			});

			await test.step("team reader (individual rights) sees Forecast but not Settings, Access, or write controls", async () => {
				await overview.lightHousePage.goToOverview();
				await overview.page.waitForURL("**/");
				await overview.search(TEAM_NAME);
				await overview.page
					.getByRole("link", { name: TEAM_NAME, exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overview.page);

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
					teamDetailPage.page.getByRole("tab", { name: "Deliveries" }),
				).not.toBeVisible();
				await expect(
					teamDetailPage.page.getByRole("button", {
						name: "Update Team Data",
					}),
				).not.toBeVisible();
			});

			await test.step("team admin (individual rights) sees Settings, Access, and management controls", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overview.search(TEAM_NAME);
				const teamAdminTeamRow = overview.page
					.getByRole("row")
					.filter({ hasText: TEAM_NAME });
				await expect(
					teamAdminTeamRow.getByRole("link", { name: "Edit" }),
				).toBeVisible();
				await expect(
					teamAdminTeamRow.getByRole("button", { name: "Delete" }),
				).toBeVisible();
				await expect(
					teamAdminTeamRow.getByRole("button", { name: "Clone" }),
				).not.toBeVisible();

				// Team Admin has no portfolio access — Project Apollo row is filtered
				// out by the backend; entire row absent.
				await overview.search(PORTFOLIO_NAME);
				await expect(
					overview.page
						.getByRole("row")
						.filter({ hasText: PORTFOLIO_NAME }),
				).toHaveCount(0);

				await overview.search(TEAM_NAME);
				await overview.page
					.getByRole("link", { name: TEAM_NAME, exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overview.page);

				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Settings" }),
				).toBeVisible();
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Access" }),
				).toBeVisible();
				await expect(
					teamDetailPage.page.getByRole("button", {
						name: "Update Team Data",
					}),
				).toBeVisible();

				const scopedAccess = new ScopedAccessPage(teamDetailPage.page);
				await scopedAccess.goToAccessTab();
				await expect(scopedAccess.groupMappingsSection).toBeVisible();
				await expect(scopedAccess.groupMappingsErrorMessage).not.toBeVisible();
			});

			await test.step("portfolio reader (individual rights) sees Deliveries read-only without admin controls", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overview.search(PORTFOLIO_NAME);
				await overview.page
					.getByRole("link", { name: PORTFOLIO_NAME, exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overview.page);

				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Deliveries" }),
				).toBeVisible();
				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Settings" }),
				).not.toBeVisible();
				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Access" }),
				).not.toBeVisible();

				const deliveriesResponsePromise = portfolioDetailPage.page.waitForResponse(
					(response) =>
						response.url().includes("/deliveries/portfolio/") &&
						response.request().method() === "GET",
				);
				const deliveriesPage = await portfolioDetailPage.goToDeliveries();
				const deliveriesResponse = await deliveriesResponsePromise;
				expect(deliveriesResponse.status()).toBe(200);

				await expect(
					deliveriesPage.page.getByText("Failed to fetch deliveries"),
				).not.toBeVisible();
				await expect(
					deliveriesPage.page.getByRole("button", { name: "Add Delivery" }),
				).not.toBeVisible();
			});

			await test.step("portfolio admin (individual rights) sees Settings, Access, and Add Delivery", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overview.search(PORTFOLIO_NAME);
				const portfolioAdminPortfolioRow = overview.page
					.getByRole("row")
					.filter({ hasText: PORTFOLIO_NAME });
				await expect(
					portfolioAdminPortfolioRow.getByRole("link", { name: "Edit" }),
				).toBeVisible();
				await expect(
					portfolioAdminPortfolioRow.getByRole("button", { name: "Delete" }),
				).toBeVisible();
				await expect(
					portfolioAdminPortfolioRow.getByRole("button", { name: "Clone" }),
				).not.toBeVisible();

				// Portfolio Admin has no team access — Team Zenith row is filtered
				// out by the backend (GetReadableTeamIdsAsync); entire row absent.
				await overview.search(TEAM_NAME);
				await expect(
					overview.page
						.getByRole("row")
						.filter({ hasText: TEAM_NAME }),
				).toHaveCount(0);

				await overview.search(PORTFOLIO_NAME);
				await overview.page
					.getByRole("link", { name: PORTFOLIO_NAME, exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overview.page);

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

				const scopedAccess = new ScopedAccessPage(portfolioDetailPage.page);
				await scopedAccess.goToAccessTab();
				await expect(scopedAccess.groupMappingsSection).toBeVisible();
				await expect(scopedAccess.groupMappingsErrorMessage).not.toBeVisible();
			});

			await test.step("System Admin switches scoped users from individual rights to SSO group mappings", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overview.search(TEAM_NAME);
				await overview.page
					.getByRole("link", { name: TEAM_NAME, exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overview.page);
				const teamScopedAccess = new ScopedAccessPage(teamDetailPage.page);
				await teamScopedAccess.goToAccessTab();

				await teamScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
				);
				await teamScopedAccess
					.getMemberRow(TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME)
					.getByRole("button", { name: "Remove" })
					.click();

				await teamScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
				);
				await teamScopedAccess
					.getMemberRow(TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME)
					.getByRole("button", { name: "Remove" })
					.click();

				await teamScopedAccess.addScopedGroupMapping(
					TestConfig.TEAMREADER_GROUP_NAME,
					"Viewer",
				);
				await teamScopedAccess.addScopedGroupMapping(
					TestConfig.TEAMADMIN_GROUP_NAME,
					"TeamAdmin",
				);

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

				await overview.lightHousePage.goToOverview();
				await overview.search(PORTFOLIO_NAME);
				await overview.page
					.getByRole("link", { name: PORTFOLIO_NAME, exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overview.page);
				const portfolioScopedAccess = new ScopedAccessPage(
					portfolioDetailPage.page,
				);
				await portfolioScopedAccess.goToAccessTab();

				await portfolioScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
				);
				await portfolioScopedAccess
					.getMemberRow(TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME)
					.getByRole("button", { name: "Remove" })
					.click();

				await portfolioScopedAccess.membersSearch.fill(
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
				);
				await portfolioScopedAccess
					.getMemberRow(TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME)
					.getByRole("button", { name: "Remove" })
					.click();

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
			});

			await test.step("team reader (group-based rights) sees Forecast but not Settings, Access, or write controls", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overview.search(TEAM_NAME);
				await overview.page
					.getByRole("link", { name: TEAM_NAME, exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overview.page);

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
					teamDetailPage.page.getByRole("button", {
						name: "Update Team Data",
					}),
				).not.toBeVisible();
			});

			await test.step("team admin (group-based rights) sees Settings, Access, and management controls", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_TEAMADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overview.search(TEAM_NAME);
				await overview.page
					.getByRole("link", { name: TEAM_NAME, exact: true })
					.click();
				const teamDetailPage = new TeamDetailPage(overview.page);

				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Settings" }),
				).toBeVisible();
				await expect(
					teamDetailPage.page.getByRole("tab", { name: "Access" }),
				).toBeVisible();
				await expect(
					teamDetailPage.page.getByRole("button", {
						name: "Update Team Data",
					}),
				).toBeVisible();

				const scopedAccess = new ScopedAccessPage(teamDetailPage.page);
				await scopedAccess.goToAccessTab();
				await expect(scopedAccess.groupMappingsSection).toBeVisible();
				await expect(scopedAccess.groupMappingsErrorMessage).not.toBeVisible();
			});

			await test.step("portfolio reader (group-based rights) sees Deliveries read-only without admin controls", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_PORTFOLIOREADER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overview.search(PORTFOLIO_NAME);
				await overview.page
					.getByRole("link", { name: PORTFOLIO_NAME, exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overview.page);

				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Deliveries" }),
				).toBeVisible();
				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Settings" }),
				).not.toBeVisible();
				await expect(
					portfolioDetailPage.page.getByRole("tab", { name: "Access" }),
				).not.toBeVisible();

				const deliveriesResponsePromise = portfolioDetailPage.page.waitForResponse(
					(response) =>
						response.url().includes("/deliveries/portfolio/") &&
						response.request().method() === "GET",
				);
				const deliveriesPage = await portfolioDetailPage.goToDeliveries();
				const deliveriesResponse = await deliveriesResponsePromise;
				expect(deliveriesResponse.status()).toBe(200);

				await expect(
					deliveriesPage.page.getByText("Failed to fetch deliveries"),
				).not.toBeVisible();
				await expect(
					deliveriesPage.page.getByRole("button", { name: "Add Delivery" }),
				).not.toBeVisible();
			});

			await test.step("portfolio admin (group-based rights) sees Settings, Access, and Add Delivery", async () => {
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_PORTFOLIOADMIN_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await overview.search(PORTFOLIO_NAME);
				await overview.page
					.getByRole("link", { name: PORTFOLIO_NAME, exact: true })
					.click();
				const portfolioDetailPage = new PortfolioDetailPage(overview.page);

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

				const scopedAccess = new ScopedAccessPage(portfolioDetailPage.page);
				await scopedAccess.goToAccessTab();
				await expect(scopedAccess.groupMappingsSection).toBeVisible();
				await expect(scopedAccess.groupMappingsErrorMessage).not.toBeVisible();
			});
		},
	);

	testWithAuth(
		"team reader hitting create routes directly sees the no-access alert and no edit form",
		async ({ loginPage }) => {
			const overview = await loginAs(
				loginPage,
				TestConfig.AUTHZ_TEST_TEAMREADER_USERNAME,
				TestConfig.AUTH_TEST_USER_PASSWORD,
			);

			// Wait for the SPA to be fully ready post-OIDC-callback so the auth cookie
			// is established before the direct page.goto navigation. Without this wait,
			// the goto can race the cookie write and hit the backend FallbackPolicy
			// challenge (redirect loop to /Account/Login).
			await overview.page
				.getByRole("link", { name: "Overview" })
				.first()
				.waitFor({ state: "visible" });

			await overview.page.goto("/teams/new");
			await expect(
				overview.page.getByTestId("team-edit-no-access-alert"),
			).toBeVisible();
			await expect(
				overview.page.getByLabel("Name", { exact: true }),
			).toHaveCount(0);

			await overview.page.goto("/portfolios/new");
			await expect(
				overview.page.getByTestId("portfolio-edit-no-access-alert"),
			).toBeVisible();
			await expect(
				overview.page.getByLabel("Name", { exact: true }),
			).toHaveCount(0);

			await overview.page.goto("/connections/new");
			await expect(
				overview.page.getByTestId("connection-edit-no-access-alert"),
			).toBeVisible();
		},
	);
});
