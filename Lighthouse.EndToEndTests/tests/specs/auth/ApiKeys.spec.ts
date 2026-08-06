import path from "node:path";
import test, {
	type APIRequestContext,
	request as apiRequest,
} from "@playwright/test";
import { TestConfig } from "../../../playwright.config";
import { expect, testWithAuth } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { LighthousePage } from "../../models/app/LighthousePage";
import { BlockedPage } from "../../models/auth/BlockedPage";
import type { LoginPage } from "../../models/auth/LoginPage";
import { RbacSettingsPage } from "../../models/auth/rbac/RbacSettingsPage";
import { OverviewPage } from "../../models/overview/OverviewPage";

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

const WHEN_WILL_THIS_BE_DONE_SCENARIO = 0;
const IN_SCOPE_TEAM = "Team Zenith";
const OUT_OF_SCOPE_PORTFOLIO = "Project Apollo";

async function loginWithValidLicense(
	loginPage: LoginPage,
	username: string = TestConfig.AUTH_TEST_USER_USERNAME,
): Promise<OverviewPage> {
	const keycloakLoginPage = await loginPage.clickSignIn();
	const overviewPage = await keycloakLoginPage.login(
		username,
		TestConfig.AUTH_TEST_USER_PASSWORD,
	);

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
): Promise<OverviewPage> {
	await overview.lightHousePage.logout();
	await overview.page.context().clearCookies();
	const lighthousePage = new LighthousePage(overview.page);
	const loginPage = await lighthousePage.openWithAuth();
	return loginWithValidLicense(loginPage, username);
}

async function findEntityId(
	request: APIRequestContext,
	collectionPath: string,
	name: string,
): Promise<number> {
	const response = await request.get(collectionPath);
	expect(response.status()).toBe(200);

	const entities = (await response.json()) as { id: number; name: string }[];
	const match = entities.find((entity) => entity.name === name);

	expect(
		match,
		`'${name}' should be seeded in ${collectionPath}`,
	).toBeDefined();
	return (match as { id: number }).id;
}

/**
 * A request context that carries ONLY the API key.
 *
 * Deliberately not `page.request`: that shares the browser's cookie jar, so an
 * out-of-scope call would still succeed on the logged-in session and the
 * refusal — the half of this that catches a regression — would be invisible.
 */
function apiKeyClient(apiKey: string): Promise<APIRequestContext> {
	return apiRequest.newContext({
		baseURL: TestConfig.LighthouseUrl,
		ignoreHTTPSErrors: true,
		extraHTTPHeaders: { "X-Api-Key": apiKey },
	});
}

test.describe("@auth API key E2E", () => {
	testWithAuth(
		"api key scoped to one team reads that team and is refused everywhere else",
		async ({ loginPage }) => {
			let overview = await loginWithValidLicense(loginPage);
			let scopedApiKey = "";
			let inScopeTeamId = 0;
			let outOfScopePortfolioId = 0;

			await test.step("assign the first System Admin and map the SSO admin group", async () => {
				const settingsPage = await overview.lightHousePage.goToSettings();
				const rbac = new RbacSettingsPage(settingsPage.page);
				await rbac.goToAccessTab();

				// Wait for the tab to settle on one of its two shapes before deciding:
				// an unchecked isVisible() here reads "no banner yet" as "already
				// bootstrapped" and silently skips the bootstrap.
				await rbac.bootstrapBanner
					.or(rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME))
					.first()
					.waitFor({ state: "visible" });

				if (await rbac.bootstrapBanner.isVisible()) {
					await rbac.becomeFirstSystemAdmin();
				}

				// The SSO group mapping editor only renders once a System Admin exists.
				await expect(
					rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME),
				).toBeVisible();

				await rbac.addSystemAdminGroupMapping(
					TestConfig.SYSTEMADMIN_GROUP_NAME,
				);
				await expect(
					rbac.getGroupMappingRow(TestConfig.SYSTEMADMIN_GROUP_NAME),
				).toBeVisible();
			});

			await test.step("seed the demo data the key scope will point at", async () => {
				await loadDemoScenario(
					overview.page.request,
					WHEN_WILL_THIS_BE_DONE_SCENARIO,
				);
				await waitForBackgroundUpdates(overview.page.request);

				inScopeTeamId = await findEntityId(
					overview.page.request,
					"/api/latest/teams",
					IN_SCOPE_TEAM,
				);
				outOfScopePortfolioId = await findEntityId(
					overview.page.request,
					"/api/latest/portfolios",
					OUT_OF_SCOPE_PORTFOLIO,
				);
			});

			await test.step("switch to a group-mapped System Admin, whose keys are actually narrowable", async () => {
				// test@user.com is the configured emergency System Admin, and that
				// bypass short-circuits ahead of the per-key scope intersection
				// (RbacAdministrationService.CanManageRbacAsync) — a key owned by it can
				// never be narrowed. The group-mapped admin is an ordinary System Admin.
				overview = await switchUser(
					overview,
					TestConfig.AUTHZ_TEST_SYSTEMADMIN_USERNAME,
				);
			});

			await test.step("create an API key restricted to read access on one team", async () => {
				const settingsPage = await overview.lightHousePage.goToSettings();
				const apiKeysPage = await settingsPage.goToApiKeys();
				const dialog = await apiKeysPage.openCreateDialog();

				await dialog.setName(`e2e-${IN_SCOPE_TEAM.toLowerCase()}-reader`);
				await dialog.expandScopes();
				await dialog.addScopeRow();
				await dialog.setScopeRow(0, {
					scopeType: "Team",
					access: "Read access",
					target: IN_SCOPE_TEAM,
				});

				scopedApiKey = await dialog.createAndRevealKey();
				expect(scopedApiKey).not.toBe("");

				await dialog.clickDone();
			});

			await test.step("the key reads its own team and is refused the portfolio outside its scope", async () => {
				// Control first, through the logged-in session: the portfolio really is
				// there, so the refusal below is a refusal and not a 404 for something
				// that was never seeded.
				const control = await overview.page.request.get(
					`/api/latest/portfolios/${outOfScopePortfolioId}`,
				);
				expect(control.status()).toBe(200);

				const keyClient = await apiKeyClient(scopedApiKey);
				try {
					const inScope = await keyClient.get(
						`/api/latest/teams/${inScopeTeamId}`,
					);
					expect(inScope.status()).toBe(200);

					// A read the key has no scope for is answered as if the portfolio did
					// not exist. This is the direction that matters: effective permissions
					// resolve off the principal, not the scheme, so a dropped api_key_id
					// claim would silently widen the key to its owner's full reach and
					// turn this into a 200.
					const outOfScope = await keyClient.get(
						`/api/latest/portfolios/${outOfScopePortfolioId}`,
					);
					expect(outOfScope.status()).toBe(404);
				} finally {
					await keyClient.dispose();
				}
			});
		},
	);
});
