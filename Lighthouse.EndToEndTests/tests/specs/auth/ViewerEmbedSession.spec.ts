import path from "node:path";
import test, { request as apiRequest } from "@playwright/test";
import { TestConfig } from "../../../playwright.config";
import { expect, testWithAuth } from "../../fixutres/LighthouseFixture";
import { LighthousePage } from "../../models/app/LighthousePage";
import { BlockedPage } from "../../models/auth/BlockedPage";
import { EmbedEntryPage } from "../../models/auth/EmbedEntryPage";
import type { LoginPage } from "../../models/auth/LoginPage";
import { RbacSettingsPage } from "../../models/auth/rbac/RbacSettingsPage";
import type { OverviewPage } from "../../models/overview/OverviewPage";

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

function newNonce(): string {
	return Array.from(crypto.getRandomValues(new Uint8Array(32)))
		.map((byte) => byte.toString(16).padStart(2, "0"))
		.join("");
}

async function ensurePremiumLicense(
	loginPage: LoginPage,
): Promise<OverviewPage> {
	const keycloakLoginPage = await loginPage.clickSignIn();
	const overviewPage = await keycloakLoginPage.login(
		TestConfig.AUTH_TEST_USER_USERNAME,
		TestConfig.AUTH_TEST_USER_PASSWORD,
	);

	const overviewLink = overviewPage.lighthousePage.overviewLink;
	const blockedTitle = overviewPage.page.getByText("LighthousePremium License");

	await Promise.any([
		overviewLink.waitFor({ state: "visible" }),
		blockedTitle.waitFor({ state: "visible" }),
	]);

	if (await blockedTitle.isVisible()) {
		const blockedPage = new BlockedPage(overviewPage.page);
		return await blockedPage.uploadLicense(LICENSE_FILE_PATH);
	}

	return overviewPage;
}

/**
 * ADR-137 D49/D60: `/embed/start` grants only to a viewer who holds a readable scope, and
 * `RbacAdministrationService.IsEnforcementGateSatisfiedAsync` requires a real System Admin ROW
 * before any scope reads as held. `Authorization__EmergencySystemAdminSubjects__0` is set in
 * `ci_verifyauth.yml` and does not help — the gate short-circuits ahead of the emergency bypass.
 * Bootstrapping through the UI is what every other auth spec does; doing it here too is what
 * stops this one depending on `EmbedSessionAndApiKeys.spec.ts` sorting first.
 */
async function ensureFirstSystemAdmin(
	overviewPage: OverviewPage,
): Promise<void> {
	const settingsPage = await overviewPage.lighthousePage.goToSettings();
	const rbac = new RbacSettingsPage(settingsPage.page);
	await rbac.goToAccessTab();

	// Wait for the tab to settle on one of its two shapes before deciding: an unchecked
	// isVisible() reads "no banner yet" as "already bootstrapped" and silently skips the bootstrap.
	await rbac.bootstrapBanner
		.or(rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME))
		.first()
		.waitFor({ state: "visible" });

	if (await rbac.bootstrapBanner.isVisible()) {
		await rbac.becomeFirstSystemAdmin();
	}

	await expect(
		rbac.getUserRow(TestConfig.AUTH_TEST_USER_USERNAME),
	).toBeVisible();
}

test.describe("@auth viewer-identity embed session E2E", () => {
	// Serial: the licence upload is a precondition for the skeleton, and both halves talk to the
	// same instance.
	test.describe.configure({ mode: "serial" });

	testWithAuth(
		"the instance is licensed, and someone holds a readable scope",
		async ({ loginPage }) => {
			const overviewPage = await ensurePremiumLicense(loginPage);

			await expect(overviewPage.lighthousePage.overviewLink).toBeVisible();

			await ensureFirstSystemAdmin(overviewPage);
		},
	);

	// The one walking skeleton for this flow (ADR-137). It is the first thing in this epic that is
	// genuinely end-to-end testable: hop 1 is a real OIDC login against the real Keycloak, so nothing
	// here is simulated. What it deliberately does not cover is Forge — the modal, router.open, a
	// nested partitioned cookie — which needs two registrable domains and lives in slice 02.
	test("a viewer signs in at top level and the frame belongs to them", async ({
		browser,
	}) => {
		const nonce = newNonce();
		let entryUrl = "";

		const signInContext = await browser.newContext({
			ignoreHTTPSErrors: true,
		});

		try {
			const signInPage = await signInContext.newPage();
			const embedEntryPage = new EmbedEntryPage(signInPage);

			await test.step("the sign-in tab challenges the instance's own identity provider", async () => {
				const keycloakLoginPage = await embedEntryPage.openSignInTab(nonce);
				await keycloakLoginPage.login(
					TestConfig.AUTH_TEST_USER_USERNAME,
					TestConfig.AUTH_TEST_USER_PASSWORD,
				);

				await expect(embedEntryPage.signInCompleteHeading).toBeVisible();
			});

			await test.step("the resolver, holding no credential at all, polls the outcome back", async () => {
				// Deliberately not signInPage.request: the Forge resolver is a backend function with
				// no cookie, and a shared jar would hide a handshake that only works when authenticated.
				const resolver = await apiRequest.newContext({
					baseURL: TestConfig.LighthouseUrl,
					ignoreHTTPSErrors: true,
				});

				try {
					const handshake = await resolver.get(
						`/api/v1/embed/handshake/${nonce}`,
					);
					expect(handshake.status()).toBe(200);

					const outcome = (await handshake.json()) as {
						token?: string;
						refusalCode?: string;
					};
					expect(outcome.refusalCode).toBeUndefined();
					expect(outcome.token).toBeTruthy();

					entryUrl = `/embed/enter?token=${encodeURIComponent(outcome.token as string)}`;
				} finally {
					await resolver.dispose();
				}
			});
		} finally {
			await signInContext.close();
		}

		// A brand-new context, so there is no Lighthouse session and no Keycloak SSO cookie to fall
		// back on: whatever renders below renders on the strength of the redeemed token alone.
		const frameContext = await browser.newContext({ ignoreHTTPSErrors: true });
		await frameContext.addInitScript(() => {
			try {
				localStorage.setItem(
					"lighthouse-hide-all-update-notifications",
					"true",
				);
			} catch {
				// localStorage unavailable in some contexts; ignore.
			}
		});

		try {
			const framePage = await frameContext.newPage();

			await test.step("the frame renders as the viewer, not as a shared key", async () => {
				await framePage.goto(entryUrl);

				const lighthousePage = new LighthousePage(framePage);
				await expect(lighthousePage.overviewLink).toBeVisible();

				// D47's evidence: the frame names THIS viewer, not merely somebody. That their
				// permissions are their own is asserted on scopes in ViewerEmbedSessionJourneyTests,
				// where a fixture can hold two viewers and this level cannot.
				await expect(lighthousePage.currentUserDisplay).toContainText(
					TestConfig.AUTH_TEST_USER_DISPLAY_NAME,
				);
			});

			await test.step("and the same link is refused the second time", async () => {
				await framePage.goto(entryUrl);

				await expect(
					new EmbedEntryPage(framePage).refusalHeading,
				).toBeVisible();
			});
		} finally {
			await frameContext.close();
		}
	});
});
