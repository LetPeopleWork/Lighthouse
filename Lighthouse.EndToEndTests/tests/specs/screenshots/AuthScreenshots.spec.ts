import path from "node:path";
import test, { expect } from "@playwright/test";
import { TestConfig } from "../../../playwright.config";
import { testWithAuth } from "../../fixutres/LighthouseFixture";
import {
	takeDialogScreenshot,
	takePageScreenshot,
} from "../../helpers/screenshots";
import { LighthousePage } from "../../models/app/LighthousePage";
import { BlockedPage } from "../../models/auth/BlockedPage";
import type { LoginPage } from "../../models/auth/LoginPage";
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

async function loginAsWithLicense(
	loginPage: LoginPage,
	username: string,
	password: string,
): Promise<OverviewPage> {
	const keycloakPage = await loginPage.clickSignIn();
	const overviewPage = await keycloakPage.login(username, password);

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

test.describe("@screenshot @auth", () => {
	testWithAuth(
		"Take @screenshot @auth of RBAC settings page",
		async ({ loginPage }) => {
			const overview = await loginAsWithLicense(
				loginPage,
				TestConfig.AUTH_TEST_USER_USERNAME,
				TestConfig.AUTH_TEST_USER_PASSWORD,
			);

			const settingsPage = await overview.lightHousePage.goToSettings();
			const rbac = await settingsPage.goToRbacSettings();

			await expect(rbac.bootstrapBanner.or(rbac.usersTable)).toBeVisible();

			if (await rbac.bootstrapBanner.isVisible()) {
				await rbac.becomeFirstSystemAdmin();
				await expect(rbac.usersTable).toBeVisible();
			}

			await takePageScreenshot(rbac.page, "settings/rbac.png");
		},
	);

	testWithAuth(
		"Take @screenshot @auth of RBAC group-mapping add controls",
		async ({ loginPage }) => {
			const overview = await loginAsWithLicense(
				loginPage,
				TestConfig.AUTH_TEST_USER_USERNAME,
				TestConfig.AUTH_TEST_USER_PASSWORD,
			);

			const settingsPage = await overview.lightHousePage.goToSettings();
			const rbac = await settingsPage.goToRbacSettings();

			if (await rbac.bootstrapBanner.isVisible()) {
				await rbac.becomeFirstSystemAdmin();
				await expect(rbac.usersTable).toBeVisible();
			}

			const groupMappingsSection = rbac.groupMappingsTable
				.locator("..")
				.locator("..");
			await expect(rbac.addGroupMappingButton).toBeVisible();
			await takeDialogScreenshot(
				groupMappingsSection,
				"settings/rbac_groupmapping.png",
			);
		},
	);

	testWithAuth(
		"Take @screenshot @auth of API Keys settings page",
		async ({ loginPage }) => {
			const overview = await loginAsWithLicense(
				loginPage,
				TestConfig.AUTH_TEST_USER_USERNAME,
				TestConfig.AUTH_TEST_USER_PASSWORD,
			);

			const settingsPage = await overview.lightHousePage.goToSettings();
			const apiKeys = await settingsPage.goToApiKeys();

			await expect(apiKeys.panel).toBeVisible();
			await expect(apiKeys.createButton).toBeVisible();

			await takePageScreenshot(apiKeys.page, "settings/apikeys.png");
		},
	);

	testWithAuth(
		"Take @screenshot @auth of API Key create dialog with scope-row builder",
		async ({ loginPage }) => {
			const overview = await loginAsWithLicense(
				loginPage,
				TestConfig.AUTH_TEST_USER_USERNAME,
				TestConfig.AUTH_TEST_USER_PASSWORD,
			);

			const settingsPage = await overview.lightHousePage.goToSettings();
			const apiKeys = await settingsPage.goToApiKeys();
			const createDialog = await apiKeys.openCreateDialog();

			await createDialog.setName("Demo API Key");
			await createDialog.expandScopes();
			await createDialog.addScopeRow();
			await expect(createDialog.scopeRowList).toBeVisible();

			await takeDialogScreenshot(
				createDialog.dialog,
				"settings/apikeys_create.png",
			);
		},
	);
});
