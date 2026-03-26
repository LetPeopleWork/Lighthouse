import path from "node:path";
import test from "@playwright/test";
import { TestConfig } from "../../../playwright.config";
import { expect, testWithAuth } from "../../fixutres/LighthouseFixture";
import { takeDialogScreenshot as takeElementScreenshot } from "../../helpers/screenshots";
import { LighthousePage } from "../../models/app/LighthousePage";
import { BlockedPage } from "../../models/auth/BlockedPage";
import type { LoginPage } from "../../models/auth/LoginPage";
import { MisconfiguredPage } from "../../models/auth/MisconfiguredPage";
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

async function loginWithoutValidLicense(
	loginPage: LoginPage,
): Promise<BlockedPage> {
	let overviewPage = await loginWithValidLicense(loginPage);
	await overviewPage.lightHousePage.clearLicense();

	const newLoginPage = await overviewPage.lightHousePage.logout();
	const keycloakLoginPage = await newLoginPage.clickSignIn();
	overviewPage = await keycloakLoginPage.login(
		TestConfig.AUTH_TEST_USER_USERNAME,
		TestConfig.AUTH_TEST_USER_PASSWORD,
	);

	const blockedPage = new BlockedPage(overviewPage.page);
	return blockedPage;
}

async function loginWithValidLicense(
	loginPage: LoginPage,
): Promise<OverviewPage> {
	const keycloakLoginPage = await loginPage.clickSignIn();
	const overviewPage = await keycloakLoginPage.login(
		TestConfig.AUTH_TEST_USER_USERNAME,
		TestConfig.AUTH_TEST_USER_PASSWORD,
	);

	const overviewLink = overviewPage.page.getByRole("link", {
		name: "Overview",
	});
	const blockedTitle = overviewPage.page.getByText("LighthousePremium License");
	const misconfiguredTitle = loginPage.page.getByText(
		"Authentication Misconfigured",
	);

	await Promise.any([
		overviewLink.waitFor({ state: "visible" }),
		blockedTitle.waitFor({ state: "visible" }),
		misconfiguredTitle.waitFor({ state: "visible" }),
	]);

	if (await blockedTitle.isVisible()) {
		const blockedPage = new BlockedPage(loginPage.page);
		return await blockedPage.uploadLicense(LICENSE_FILE_PATH);
	}

	return new OverviewPage(
		overviewPage.page,
		new LighthousePage(overviewPage.page),
	);
}

test.describe("@Auth E2E", () => {
	testWithAuth(
		"should show login page when not authenticated",
		async ({ loginPage }) => {
			await expect(loginPage.container).toBeVisible();
			await expect(loginPage.signInButton).toBeVisible();

			await takeElementScreenshot(
				loginPage.container,
				"authentication/signin.png",
			);
		},
	);

	testWithAuth(
		"should enter blocked mode after login without premium license",
		async ({ loginPage }) => {
			const blockedPage = await loginWithoutValidLicense(loginPage);
			await expect(blockedPage.container).toBeVisible();
			await expect(blockedPage.uploadButton).toBeVisible();
			await expect(blockedPage.logoutButton).toBeVisible();

			await takeElementScreenshot(
				blockedPage.container,
				"authentication/blocked.png",
			);
		},
	);

	testWithAuth(
		"should recover from blocked mode by uploading a valid license",
		async ({ loginPage }) => {
			const blockedPage = await loginWithoutValidLicense(loginPage);

			const overviewPage = await blockedPage.uploadLicense(LICENSE_FILE_PATH);

			// After upload the page reloads and transitions to authenticated app shell
			await expect(
				overviewPage.page.getByRole("link", { name: "Overview" }),
			).toBeVisible();
		},
	);

	testWithAuth(
		"should logout and return to login page",
		async ({ loginPage }) => {
			const blockedPage = await loginWithoutValidLicense(loginPage);

			const newLoginPage = await blockedPage.clickLogout();

			// After Keycloak logout redirect chain, should return to login page
			await expect(newLoginPage.container).toBeVisible();
		},
	);

	testWithAuth(
		"should enter authenticated app shell when premium license exists",
		async ({ loginPage }) => {
			const overviewPage = await loginWithValidLicense(loginPage);

			await expect(
				overviewPage.page.getByRole("link", { name: "Overview" }),
			).toBeVisible();

			// Verify the logout button is available in the authenticated header
			await expect(
				overviewPage.page.getByTestId("logout-button"),
			).toBeVisible();
		},
	);

	testWithAuth(
		"should show session-expired page when session becomes invalid",
		async ({ loginPage, page }) => {
			await page.clock.install();

			const overviewPage = await loginWithValidLicense(loginPage);

			const sessionExpiredPage =
				await overviewPage.lightHousePage.clearCookies();

			await page.clock.fastForward(61_000);

			await expect(sessionExpiredPage.container).toBeVisible();
			await expect(sessionExpiredPage.signInAgainButton).toBeVisible();

			await takeElementScreenshot(
				sessionExpiredPage.container,
				"authentication/session_expired.png",
			);
		},
	);

	testWithAuth(
		"should show misconfigured page when auth configuration is invalid",
		async ({ loginPage }) => {
			await loginPage.page.route("**/api/auth/mode", async (route) => {
				await route.fulfill({
					status: 200,
					contentType: "application/json",
					body: JSON.stringify({
						mode: "Misconfigured",
						misconfigurationMessage:
							"Authority is required when authentication is enabled.",
					}),
				});
			});

			const misconfiguredPage = new MisconfiguredPage(loginPage.page);

			await expect(misconfiguredPage.container).toBeVisible();
			await expect(misconfiguredPage.message).toContainText("Authority");

			await takeElementScreenshot(
				misconfiguredPage.container,
				"authentication/misconfigured.png",
			);
		},
	);
});
