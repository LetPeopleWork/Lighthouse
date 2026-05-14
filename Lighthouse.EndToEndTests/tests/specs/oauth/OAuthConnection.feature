# OAuth Connection scenarios — Gherkin documentation form
# Feature: work-tracking-oauth-authentication
# Executable form: ./OAuthConnection.spec.ts (Playwright). Same scenario titles.
# Wave: DISTILL
# Date: 2026-05-14 (revised after final review gate)
#
# Scenarios that test implementation invariants (not user-observable behaviour) have
# been moved to backend integration tests where they execute faster and without
# browser brittleness. The user-facing acceptance coverage is unchanged. Migrated
# scenarios and their new homes:
#
#   - "Provider-agnostic abstraction (stub provider works without controller changes)"
#       → Lighthouse.Backend.Tests/API/Integration/OAuthProviderAbstractionIntegrationTest.cs
#   - "Invalid state token on callback is rejected (CSRF)"
#       → Lighthouse.Backend.Tests/API/Integration/OAuthCallbackCsrfIntegrationTest.cs
#   - "Concurrent syncs trigger at most one refresh (single-flight)"
#       → Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthRefreshSingleFlightTest.cs
#   - "Standalone exposes no /api/oauth/* route"
#       → Lighthouse.Backend.Tests/API/Integration/OAuthStandaloneModeRouteRejectionIntegrationTest.cs
#
# Walking Skeleton Strategy: D (Configurable per environment matrix).
#   - PR / main CI: scenarios run against StubOAuthProvider via test DI (no outbound HTTPS to real IdPs)
#   - Pre-release smoke (ci_oauth_integration_smoke.yml): the @requires_external scenarios run against real Atlassian + Entra ID sandboxes
#
# Tag legend:
#   @walking_skeleton      — the one e2e-thin-slice that proves the full pipeline (driving adapter → driven adapters → DB)
#   @driving_adapter       — exercises the HTTP entry point via real network call (not a service-function invocation)
#   @real-io               — uses real DB, real ICryptoService, real file system
#   @in-memory             — uses StubOAuthProvider (no real IdP HTTPS)
#   @requires_external     — needs real Atlassian / Entra ID sandbox credentials; skipped in PR CI
#   @US-N                  — traceability to feature-delta.md user story N
#   @kpi-OUT-N             — verifies the named outcome (links to docs/product/kpi-contracts.yaml)
#   @error                 — error-path scenario
#   @adapter-integration   — adapter Mandate 6 coverage (real I/O for a specific driven adapter)


Feature: OAuth-authenticated work-tracking connections (Jira + Azure DevOps)
  As a connector-admin (Premium-licensed, server-mode, SystemAdmin role)
  I want to configure Jira and Azure DevOps work-tracking connections via OAuth
  So I can govern their credentials in my organisation's IdP instead of pasting personal access tokens

  Background:
    Given Lighthouse is running in server mode with a valid Premium license
    And a SystemAdmin user is signed in
    And the StubOAuthProvider is registered for jira.oauth and ado.oauth providers
    And Lighthouse:BaseUrl is configured to "https://lighthouse-test.local"


  # ─────────────────────────────────────────────────────────────────────────
  # Slice 01 — Jira OAuth (foundation slice; folds in #4971 abstraction + #4972 BaseUrl)
  # ─────────────────────────────────────────────────────────────────────────

  @walking_skeleton @driving_adapter @real-io @in-memory @US-01 @adapter-integration @kpi-OUT-oauth-setup-success-rate
  Scenario: Jira OAuth connection is configured end-to-end and the first sync succeeds
    Given an empty database with no work-tracking connections
    When the SystemAdmin opens "New Jira connection" and selects Authentication "OAuth (Jira Cloud)"
    And pastes clientId "test-jira-client" and clientSecret "test-jira-secret"
    And clicks "Connect"
    Then the browser is redirected to the StubOAuthProvider's authorization URL
    And after the stub consent screen completes the callback URL "https://lighthouse-test.local/api/oauth/callback" is invoked
    And the connection settings page shows "Status: Connected — OAuth (Jira Cloud)"
    And the OAuthCredential row for this connection has Status=Valid
    And the next manual "Update All" against this connection issues an outbound request carrying "Authorization: Bearer <stub-access-token>"
    And the response contains at least one synced work item from the stub provider

  @driving_adapter @real-io @in-memory @US-01 @error
  Scenario: Non-Premium instance shows upgrade affordance instead of OAuth form
    Given Lighthouse is running with a Premium license that has expired
    When the SystemAdmin opens "New Jira connection"
    Then "OAuth (Jira Cloud)" appears in the authentication dropdown
    But selecting it shows an "Upgrade to Premium" panel instead of the OAuth form fields
    And POST /api/oauth/jira.oauth/connect returns HTTP 402 with body referencing the premium gate

  @driving_adapter @real-io @in-memory @US-01 @error
  Scenario: Lighthouse:BaseUrl unset triggers the callback-URL warning
    Given Lighthouse:BaseUrl is not configured
    When the SystemAdmin opens the Jira OAuth form
    Then a non-blocking warning is shown: "Your callback URL may be incorrect. Set Lighthouse:BaseUrl …"
    And the callback URL still renders (falling back to request origin) so the admin can proceed

  # Note: "Provider-agnostic abstraction" + "Invalid state token rejected" are
  # implementation invariants moved to Lighthouse.Backend.Tests/API/Integration/
  # (see file header). The user-observable Slice 01 coverage above is unchanged.


  # ─────────────────────────────────────────────────────────────────────────
  # Slice 02 — Token refresh (US-02)
  # ─────────────────────────────────────────────────────────────────────────

  @driving_adapter @real-io @in-memory @US-02 @kpi-OUT-oauth-refresh-success-rate
  Scenario: Access token is refreshed silently before its expiry
    Given a Jira OAuth connection with an OAuthCredential whose ExpiresAt is 3 minutes from now
    When a scheduled work-item sync fires for a team using that connection
    Then the StubOAuthProvider's refresh endpoint is invoked exactly once
    And the OAuthCredential row is updated with a new AccessToken / RefreshToken / ExpiresAt
    And the outbound work-item sync succeeds carrying "Authorization: Bearer <new-stub-token>"
    And no reconnect banner is shown to any user

  @driving_adapter @real-io @in-memory @US-02 @error
  Scenario: Failed refresh marks the credential RefreshFailed and surfaces the reconnect banner
    Given a Jira OAuth connection whose refresh token the StubOAuthProvider will reject
    When a scheduled work-item sync fires for a team using that connection
    Then the OAuthCredential row becomes Status=RefreshFailed
    And the next GET /api/latest/worktrackingsystemconnections response carries "requiresReconnect": true for this connection
    And the connections list page renders a yellow banner with text matching "Reconnect required"
    And the failing sync is aborted cleanly (no uncaught exception in logs)

  # Note: "Concurrent syncs trigger at most one refresh (single-flight)" is an
  # implementation invariant (concurrency under load) moved to
  # Lighthouse.Backend.Tests/Services/Implementation/OAuth/OAuthRefreshSingleFlightTest.cs.

  @driving_adapter @real-io @in-memory @US-02
  Scenario: Reconnecting from the banner clears RefreshFailed and restores Status=Valid
    Given a Jira OAuth connection with OAuthCredential Status=RefreshFailed
    When the SystemAdmin clicks "Reconnect" on the connection's banner
    And completes the StubOAuthProvider consent dance
    Then the OAuthCredential row Status returns to Valid
    And the reconnect banner is no longer rendered on the connections list

  # Folded into Slice 02 per OQ-DV1 resolution (2026-05-14). The data this slice
  # already produces (setup attempts, refresh outcomes, RefreshFailed transitions)
  # is exactly what the tile surfaces — splitting into Slice 05 would have meant
  # duplicating the test setup.
  @driving_adapter @real-io @in-memory @US-02 @kpi-OUT-oauth-setup-success-rate @kpi-OUT-oauth-refresh-success-rate
  Scenario: OAuth Health tile shows current setup-success and refresh-success rates to the SystemAdmin
    Given the database has seeded log-event data for 5 successful OAuth setups and 1 failed setup in the last 30 days
    And 100 successful refreshes and 1 failed refresh in the last 7 days for a single connection
    When the SystemAdmin opens the Connections settings page
    Then the page renders an "OAuth Health" tile with:
      | metric                          | shown value |
      | Setup success rate (30 days)    | 83%         |
      | Refresh success rate (7 days)   | 99%         |
      | Stale RefreshFailed connections | 0           |
    And a non-SystemAdmin user opening the same page sees no OAuth Health tile
    And a SystemAdmin on a non-Premium instance sees the tile replaced by an "Upgrade to Premium" affordance


  # ─────────────────────────────────────────────────────────────────────────
  # Slice 03 — Azure DevOps OAuth (US-03)
  # ─────────────────────────────────────────────────────────────────────────

  @driving_adapter @real-io @in-memory @US-03 @adapter-integration
  Scenario: Azure DevOps OAuth connection is configured end-to-end
    Given an empty database with no work-tracking connections
    When the SystemAdmin opens "New Azure DevOps connection" and selects Authentication "OAuth (Azure DevOps)"
    And pastes clientId "test-ado-client" and clientSecret "test-ado-secret"
    And clicks "Connect"
    Then the browser is redirected to the StubOAuthProvider's authorization URL
    And the callback URL "https://lighthouse-test.local/api/oauth/callback" is invoked
    And the connection page shows "Status: Connected — OAuth (Azure DevOps)" with scope "vso.work_write" listed
    And the next "Update All" against this connection carries Bearer auth

  @driving_adapter @real-io @in-memory @US-03 @error
  Scenario: ADO OAuth form warns when BaseUrl is HTTP (Azure DevOps requires HTTPS at registration time)
    Given Lighthouse:BaseUrl is configured to "http://lighthouse-test.local" (HTTP, not HTTPS)
    When the SystemAdmin opens the ADO OAuth form
    Then a warning is shown: "Azure DevOps requires HTTPS callback URLs in production"
    And the form remains usable (no input is disabled)

  @driving_adapter @real-io @in-memory @US-03
  Scenario: Adding the ADO provider did not require changes to the controller or persistence (DESIGN AC #5)
    # NOTE: this is a structural assertion — verified by diff review at PR time, not by Playwright.
    # The .spec.ts companion records the assertion as a test.skip() with a TODO referencing
    # the diff-review gate in the slice-03 brief.
    Given the AdoOAuthProvider is implemented as IOAuthProvider only
    Then the diff between slice-01 main and slice-03 PR shows changes ONLY in
      """
      Lighthouse.Backend/Services/Implementation/OAuth/Providers/AdoOAuthProvider.cs
      Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodSchema.cs
      Lighthouse.Backend/Program.cs (DI registration only)
      docs/* (new ADO setup page)
      Lighthouse.Frontend/src/components/Common/Connections/AuthMethodDropdown.tsx (label only)
      """
    And no diff in OAuthController, OAuthService, OAuthCredential, OAuthTokenRefreshService, or the EF context


  # ─────────────────────────────────────────────────────────────────────────
  # Slice 04 — Standalone-mode guard (US-04, frontend-only)
  # ─────────────────────────────────────────────────────────────────────────

  @real-io @in-memory @US-04
  Scenario: Standalone (Tauri) mode renders the OAuth dropdown option disabled with explanatory tooltip
    Given the app is running in standalone (Tauri) mode
    When the SystemAdmin opens any work-tracking-system connector form
    Then every "OAuth (...)" entry in the authentication-method dropdown is rendered with the disabled attribute set
    And hovering a disabled OAuth entry shows the tooltip "OAuth authentication requires the server version of Lighthouse. Learn more →"
    And the "Learn more" link points to the docs page "/docs/server-vs-standalone"

  # Note: "Standalone exposes no /api/oauth/* route" is an implementation invariant
  # (route-table absence) moved to Lighthouse.Backend.Tests/API/Integration/
  # OAuthStandaloneModeRouteRejectionIntegrationTest.cs. The user-facing scenario
  # above (disabled dropdown + tooltip) is the user-observable acceptance coverage.


  # ─────────────────────────────────────────────────────────────────────────
  # Pre-release smoke — real IdPs (ci_e2e.yml `oauth-smoke` job only)
  # ─────────────────────────────────────────────────────────────────────────

  @driving_adapter @real-io @requires_external @US-01 @smoke
  Scenario: Smoke — real Atlassian Cloud sandbox 3LO flow completes against the live IdP
    Given the test environment has JIRA_OAUTH_SANDBOX_CLIENT_ID and JIRA_OAUTH_SANDBOX_CLIENT_SECRET set
    And the Atlassian sandbox app is registered with redirect URI {OAUTH_SMOKE_BASE_URL}/api/oauth/callback
    When the smoke harness completes the full 3LO dance against auth.atlassian.com
    Then an OAuthCredential row exists with Status=Valid for the sandbox connection
    And a subsequent Jira API call returns HTTP 200 carrying the issued bearer token
    And at least one real Jira issue from the sandbox project is synced into Lighthouse

  @driving_adapter @real-io @requires_external @US-03 @smoke
  Scenario: Smoke — real Entra ID / Azure DevOps OAuth flow completes against the live IdP
    Given the test environment has ADO_OAUTH_SANDBOX_CLIENT_ID and ADO_OAUTH_SANDBOX_CLIENT_SECRET set
    And the Entra ID sandbox app is registered with redirect URI {OAUTH_SMOKE_BASE_URL}/api/oauth/callback
    When the smoke harness completes the OAuth dance against login.microsoftonline.com
    Then an OAuthCredential row exists with Status=Valid for the sandbox connection
    And a subsequent dev.azure.com API call returns HTTP 200 carrying the issued bearer token
    And at least one real ADO work item from the sandbox organisation is synced into Lighthouse
