import path from "node:path";
import test from "@playwright/test";
import { TestConfig } from "../../../playwright.config";
import { expect, testWithAuth } from "../../fixutres/LighthouseFixture";
import { LighthousePage } from "../../models/app/LighthousePage";
import { BlockedPage } from "../../models/auth/BlockedPage";
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

test.describe("@ProxyAuth E2E", () => {
	testWithAuth(
		"completes OIDC login through a TLS-terminating reverse proxy",
		async ({ loginPage, context }) => {
			const keycloakLoginPage = await loginPage.clickSignIn();
			let overviewPage = await keycloakLoginPage.login(
				TestConfig.AUTH_TEST_USER_USERNAME,
				TestConfig.AUTH_TEST_USER_PASSWORD,
			);

			const blockedTitle = overviewPage.page.getByText(
				"LighthousePremium License",
			);
			const overviewLink = overviewPage.page.getByRole("link", {
				name: "Overview",
			});

			await Promise.any([
				overviewLink.waitFor({ state: "visible" }),
				blockedTitle.waitFor({ state: "visible" }),
			]);

			if (await blockedTitle.isVisible()) {
				const blockedPage = new BlockedPage(overviewPage.page);
				overviewPage = await blockedPage.uploadLicense(LICENSE_FILE_PATH);
			} else {
				overviewPage = new OverviewPage(
					overviewPage.page,
					new LighthousePage(overviewPage.page),
				);
			}

			await expect(overviewLink).toBeVisible();
			expect(overviewPage.page.url()).toContain("https://localhost:8443");

			const sessionCookie = (await context.cookies()).find((cookie) =>
				cookie.name.startsWith(".Lighthouse.Session"),
			);
			expect(sessionCookie?.secure).toBe(true);
		},
	);
});
