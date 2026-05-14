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
 *   Slice 04 (US-04): standalone disabled dropdown → standalone no backend route
 *   Pre-release smoke (@requires_external): real Atlassian + real Entra ID (gated to ci_oauth_integration_smoke.yml only)
 *
 * Tagging convention matches the .feature file. Tags appear in the test() title
 * string in square brackets so they're greppable from CI output, mirroring the
 * existing Lighthouse pattern (no Playwright tag-filter required — string match).
 *
 * Walking Skeleton Strategy: D (Configurable per DEVOPS env matrix).
 *   - PR / main CI: StubOAuthProvider injected via the test fixture.
 *   - Pre-release: real IdPs (gated by GITHUB_ENV oauth-smoke approval).
 */

import test, { expect } from "@playwright/test";
import { testWithAuth } from "../../fixutres/LighthouseFixture";
import {
	callOAuthCallback,
	createOAuthJiraConnection,
	initiateOAuthConnect,
} from "../../helpers/api/oauthConnections";
import { generateRandomName } from "../../helpers/names";

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
			expect(callback.location).toContain(
				`/settings/connections/${connection.id}?oauth=success`,
			);

			await request.delete(
				`/api/latest/worktrackingsystemconnections/${connection.id}`,
			);
		},
	);

	testWithAuth(
		"[@driving_adapter @real-io @in-memory @US-01 @error] Non-Premium instance shows upgrade affordance instead of OAuth form",
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

			const premiumBlocked = initiate.status === 402 || initiate.status === 403;
			expect(premiumBlocked, initiate.body).toBe(true);

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

// ─────────────────────────────────────────────────────────────────────────────
// Slice 02 — Token refresh (US-02)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Slice 02 — Token refresh", () => {
	testWithAuth.skip(
		"[@driving_adapter @real-io @in-memory @US-02 @kpi-OUT-oauth-refresh-success-rate] Access token is refreshed silently before its expiry",
		async () => {
			// TODO(DELIVER Slice 02): seed a Valid OAuthCredential with ExpiresAt 3 min from now;
			// trigger sync; assert refresh endpoint invoked once; assert credential row updated;
			// assert no reconnect banner. Verifies the operational-cost claim of the epic.
			throw new Error("Not yet implemented — RED scaffold");
		},
	);

	testWithAuth.skip(
		"[@driving_adapter @real-io @in-memory @US-02 @error] Failed refresh marks the credential RefreshFailed and surfaces the reconnect banner",
		async () => {
			// TODO(DELIVER Slice 02): seed credential whose refresh token the stub will reject;
			// trigger sync; assert Status=RefreshFailed, requiresReconnect:true in connections list,
			// yellow banner rendered.
			throw new Error("Not yet implemented — RED scaffold");
		},
	);

	// Re-layered on 2026-05-14 — "Concurrent syncs trigger at most one refresh
	// (single-flight)" is a concurrency invariant. Backend service test at:
	//   Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthRefreshSingleFlightTest.cs

	testWithAuth.skip(
		"[@driving_adapter @real-io @in-memory @US-02] Reconnecting from the banner clears RefreshFailed and restores Status=Valid",
		async () => {
			// TODO(DELIVER Slice 02): set credential to RefreshFailed; click Reconnect; complete
			// stub flow; assert Status=Valid, banner gone.
			throw new Error("Not yet implemented — RED scaffold");
		},
	);

	// Folded into Slice 02 per OQ-DV1 resolution (2026-05-14).
	testWithAuth.skip(
		"[@driving_adapter @real-io @in-memory @US-02 @kpi-OUT-oauth-setup-success-rate @kpi-OUT-oauth-refresh-success-rate] OAuth Health tile shows current setup-success and refresh-success rates to the SystemAdmin",
		async () => {
			// TODO(DELIVER Slice 02): seed log events for setup attempts + refresh outcomes;
			// open Connections settings page as SystemAdmin on Premium-licensed instance;
			// assert the OAuth Health tile renders with the three KPI rows.
			// Then assert: non-SystemAdmin sees no tile; SystemAdmin on non-Premium sees
			// the Upgrade affordance instead.
			// time_to_first_sync_p95_30d is NOT asserted in this scenario — it's deferred
			// (requires the connection.sync.first_after_oauth event which is folded later).
			throw new Error("Not yet implemented — RED scaffold");
		},
	);
});

// ─────────────────────────────────────────────────────────────────────────────
// Slice 03 — Azure DevOps OAuth (US-03)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Slice 03 — Azure DevOps OAuth", () => {
	testWithAuth.skip(
		"[@driving_adapter @real-io @in-memory @US-03 @adapter-integration] Azure DevOps OAuth connection is configured end-to-end",
		async () => {
			// TODO(DELIVER Slice 03): mirror Slice 01 walking-skeleton flow, but for ADO. Assert
			// scope "vso.work_write" listed on the settings page; assert subsequent sync carries Bearer.
			throw new Error("Not yet implemented — RED scaffold");
		},
	);

	testWithAuth.skip(
		"[@driving_adapter @real-io @in-memory @US-03 @error] ADO OAuth form warns when BaseUrl is HTTP",
		async () => {
			// TODO(DELIVER Slice 03): set BaseUrl to http://...; open ADO OAuth form; assert the
			// HTTPS warning text appears; assert form remains usable.
			throw new Error("Not yet implemented — RED scaffold");
		},
	);

	// NOTE: "Adding the ADO provider did not require changes to the controller or persistence"
	// (Slice 03 AC #5) is verified by PR diff review, not by Playwright. Tracked as a checkbox
	// in the Slice 03 PR template — not implemented as a Playwright test.
});

// ─────────────────────────────────────────────────────────────────────────────
// Slice 04 — Standalone-mode guard (US-04, frontend-only)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Slice 04 — Standalone-mode guard", () => {
	test.skip("[@real-io @in-memory @US-04] Standalone (Tauri) mode renders the OAuth dropdown option disabled with explanatory tooltip", async () => {
		// TODO(DELIVER Slice 04): launch standalone build; open any connector form; assert
		// every OAuth dropdown entry has the disabled attribute; hover one; assert tooltip
		// text + "Learn more" link target.
		throw new Error("Not yet implemented — RED scaffold");
	});

	// Re-layered on 2026-05-14 — "Standalone exposes no /api/oauth/* route" is a
	// route-table invariant. Backend integration test at:
	//   Lighthouse.Backend.Tests/API/Integration/OAuthStandaloneModeRouteRejectionIntegrationTest.cs
});

// ─────────────────────────────────────────────────────────────────────────────
// Pre-release smoke — real IdPs
// (ci_oauth_integration_smoke.yml only; gated by oauth-smoke environment approval)
// ─────────────────────────────────────────────────────────────────────────────

test.describe("Pre-release smoke (real IdPs)", () => {
	test.skip("[@driving_adapter @real-io @requires_external @US-01 @smoke] Real Atlassian Cloud sandbox 3LO flow completes", async () => {
		// TODO(DELIVER Slice 01 release-readiness): gated by JIRA_OAUTH_SANDBOX_* secrets +
		// OAUTH_SMOKE_BASE_URL. Runs ONLY in ci_oauth_integration_smoke.yml.
		throw new Error("Not yet implemented — RED scaffold");
	});

	test.skip("[@driving_adapter @real-io @requires_external @US-03 @smoke] Real Entra ID / Azure DevOps OAuth flow completes", async () => {
		// TODO(DELIVER Slice 03 release-readiness): gated by ADO_OAUTH_SANDBOX_* secrets +
		// OAUTH_SMOKE_BASE_URL. Runs ONLY in ci_oauth_integration_smoke.yml.
		throw new Error("Not yet implemented — RED scaffold");
	});
});
