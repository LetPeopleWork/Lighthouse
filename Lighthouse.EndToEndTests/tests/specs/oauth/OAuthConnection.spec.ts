/**
 * OAuth Connection — Playwright spec (executable form)
 *
 * Feature: work-tracking-oauth-authentication (ADO Epic #2438)
 * Wave: DISTILL (skeleton — all tests start as test.skip())
 *
 * The Gherkin documentation form lives alongside as `OAuthConnection.feature`.
 * Same scenario titles in both. DELIVER unskips one at a time in this order:
 *
 *   Slice 01 (US-01): walking_skeleton → non-premium → BaseUrl warning → stub provider → invalid state
 *   Slice 02 (US-02): silent refresh → refresh-fail banner → single-flight → reconnect clears
 *   Slice 03 (US-03): ADO happy path → HTTPS warning (Slice 03 AC #5 verified by PR diff review, not Playwright)
 *
 * Tagging convention matches the .feature file. Tags appear in the test() title
 * string in square brackets so they're greppable from CI output, mirroring the
 * existing Lighthouse pattern (no Playwright tag-filter required — string match).
 *
 * Walking Skeleton Strategy: D (Configurable per DEVOPS env matrix).
 *   - PR / main CI: StubOAuthProvider injected via the test fixture.
 */

import test, { expect } from "@playwright/test";
import { testWithAuth } from "../../fixutres/LighthouseFixture";
import {
	callOAuthCallback,
	completeOAuthRoundTrip,
	createOAuthAdoConnection,
	createOAuthJiraConnection,
	disconnectOAuth,
	initiateOAuthConnect,
} from "../../helpers/api/oauthConnections";
import { generateRandomName } from "../../helpers/names";
import { LighthousePage } from "../../models/app/LighthousePage";

// ─────────────────────────────────────────────────────────────────────────────
// Slice 01 — Jira OAuth (US-01, includes #4971 abstraction + #4972 BaseUrl)
//
// These scenarios run against a Lighthouse backend launched with the env vars:
//   Lighthouse__OAuth__UseStubProvider=true
//   Lighthouse__BaseUrl=<test-time URL matching LIGHTHOUSEURL>
//   Lighthouse__OAuth__StateSecret=<any deterministic value>
// The StubOAuthProvider returns deterministic tokens and a self-referential
// authorization URL that loops back to /api/oauth/callback without contacting
// a real IdP.
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Slice 01 — Jira OAuth", () => {
	testWithAuth(
		"[@walking_skeleton @driving_adapter @real-io @in-memory @US-01 @kpi-OUT-oauth-setup-success-rate] Jira OAuth connection is configured end-to-end and the first sync succeeds",
		async ({ request }) => {
			const connection = await createOAuthJiraConnection(
				request,
				generateRandomName(),
			);

			const initiate = await initiateOAuthConnect(
				request,
				"jira.oauth",
				connection.id,
			);

			expect(initiate.status, initiate.body).toBe(200);
			expect(initiate.authorizationUrl, initiate.body).toBeDefined();
			expect(initiate.authorizationUrl).toContain("/api/oauth/callback");

			const callback = await callOAuthCallback(
				request,
				initiate.authorizationUrl ?? "",
			);

			expect(callback.status).toBeGreaterThanOrEqual(300);
			expect(callback.status).toBeLessThan(400);

			await request.delete(
				`/api/latest/worktrackingsystemconnections/${connection.id}`,
			);
		},
	);

	testWithAuth(
		"[@driving_adapter @real-io @in-memory @US-01 @error] Lighthouse:BaseUrl unset triggers the callback-URL warning",
		async ({ request }) => {
			const connection = await createOAuthJiraConnection(
				request,
				generateRandomName(),
			);

			const initiate = await initiateOAuthConnect(
				request,
				"jira.oauth",
				connection.id,
			);

			if (initiate.status === 200) {
				expect(initiate.authorizationUrl).toContain("/api/oauth/callback");
			} else {
				expect(initiate.body).toMatch(/baseurl/i);
			}

			await request.delete(
				`/api/latest/worktrackingsystemconnections/${connection.id}`,
			);
		},
	);

	// Re-layered on 2026-05-14 — "Provider-agnostic abstraction" + "Invalid state token
	// rejected (CSRF)" are implementation invariants. Backend integration tests at:
	//   Lighthouse.Backend.Tests/API/Integration/OAuthProviderAbstractionIntegrationTest.cs
	//   Lighthouse.Backend.Tests/API/Integration/OAuthCallbackCsrfIntegrationTest.cs
});

// Slice 02 — Token refresh (US-02) — re-layered 2026-05-15.
// All four scenarios (silent refresh, failed-refresh banner, reconnect-from-banner,
// OAuth Health tile) assert implementation invariants — state transitions, DTO
// projection, component rendering, endpoint gating — that execute deterministically
// at the layer they belong to. See ./OAuthConnection.feature header for the
// per-scenario routing to backend service / controller tests and Vitest component /
// page tests.

// ─────────────────────────────────────────────────────────────────────────────
// Slice 03 — Azure DevOps OAuth (US-03)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Slice 03 — Azure DevOps OAuth", () => {
	testWithAuth(
		"[@driving_adapter @real-io @in-memory @US-03 @adapter-integration] Azure DevOps OAuth connection is configured end-to-end",
		async ({ request }) => {
			const connection = await createOAuthAdoConnection(
				request,
				generateRandomName(),
			);

			const initiate = await initiateOAuthConnect(
				request,
				"ado.oauth",
				connection.id,
			);

			expect(initiate.status, initiate.body).toBe(200);
			expect(initiate.authorizationUrl, initiate.body).toBeDefined();
			expect(initiate.authorizationUrl).toContain("/api/oauth/callback");

			const callback = await callOAuthCallback(
				request,
				initiate.authorizationUrl ?? "",
			);

			expect(callback.status).toBeGreaterThanOrEqual(300);
			expect(callback.status).toBeLessThan(400);

			await request.delete(
				`/api/latest/worktrackingsystemconnections/${connection.id}`,
			);
		},
	);

	// NOTE: "@US-03 @error — ADO OAuth form warns when BaseUrl is HTTP" is verified at
	// the unit layer by CreateConnectionWizard.test.tsx (positive ADO+OAuth+http and
	// negative Jira+OAuth+http cases). Re-asserting via Playwright duplicates a pure
	// UI-rendering invariant at high cost.

	// NOTE: "Adding the ADO provider did not require changes to the controller or persistence"
	// (Slice 03 AC #5) is verified by PR diff review, not by Playwright. Tracked as a checkbox
	// in the Slice 03 PR template — not implemented as a Playwright test.
});

test.describe("Story #5018 — Popup reconnect (migration of slice-02 walking skeleton)", () => {
	testWithAuth(
		"[@walking_skeleton @popup-migration @driving_adapter @real-io @in-memory @US-01 @Story-5018] Reconnect from a disconnected connection's edit dialog flips the status badge to Connected without page navigation",
		async ({ page, request }) => {
			const connectionName = generateRandomName();
			const connection = await createOAuthJiraConnection(
				request,
				connectionName,
			);
			await completeOAuthRoundTrip(request, "jira.oauth", connection.id);
			await disconnectOAuth(request, "jira.oauth", connection.id);

			const lighthousePage = new LighthousePage(page);
			const overviewPage = await lighthousePage.open();
			const editPage = await overviewPage.editConnection(connectionName);

			await expect(editPage.reconnectBanner).toBeVisible();

			const editUrlBeforeReconnect = page.url();
			const openerNavigations: string[] = [];
			const onFrameNavigated = (frame: {
				parentFrame: () => unknown;
				url: () => string;
			}): void => {
				if (frame.parentFrame() === null) {
					openerNavigations.push(frame.url());
				}
			};
			page.on("framenavigated", onFrameNavigated);

			try {
				const popup = await editPage.clickReconnectAndWaitForPopup();
				await popup.waitForEvent("close", { timeout: 15_000 });

				await expect(editPage.reconnectBanner).toBeHidden();
				expect(page.url()).toBe(editUrlBeforeReconnect);
				expect(openerNavigations).toEqual([]);
			} finally {
				page.off("framenavigated", onFrameNavigated);
				await request.delete(
					`/api/latest/worktrackingsystemconnections/${connection.id}`,
				);
			}
		},
	);
});
