Feature: Scope picker visibility tracks RBAC enablement
  Visibility of the "Restrict scope (optional)" section in the Create API Key
  dialog is gated on whether RBAC is enforcing permissions. When RBAC is off,
  per-key scopes have no effect (RbacAdministrationService short-circuits to
  "allow" before any scope intersection runs); hiding the picker prevents the
  user from configuring a value that will be silently ignored.

  Background:
    Given Lighthouse is running with authentication enabled
    And the authenticated user is allowed to open the API Keys settings tab
    And the authenticated user has opened the "API Keys" settings tab

  @in-memory @milestone-1 @driving_adapter
  Scenario: M1.1 Scope picker is visible when RBAC is enabled (regression pin)
    Given RBAC is enabled in the running deployment
    When the user clicks "New API Key"
    Then the Create API Key dialog is open
    And the "Restrict scope (optional)" section is present and collapsed by default
    When the user expands the "Restrict scope (optional)" section
    Then the scope row list control is rendered
    And the "Add scope" button is enabled

  @in-memory @milestone-1
  Scenario: M1.2 Scope picker is hidden when RBAC is disabled
    Given RBAC is disabled in the running deployment
    When the user clicks "New API Key"
    Then the Create API Key dialog is open
    And the "Restrict scope (optional)" section is NOT present in the dialog
    And no scope row list control is rendered
    And no "Add scope" button is rendered

  @in-memory @milestone-1 @error
  Scenario: M1.3 Scope picker is hidden when the authorization summary fetch fails
    Given RBAC enablement cannot be determined because the authorization summary request fails
    When the user clicks "New API Key"
    Then the Create API Key dialog is open
    And the "Restrict scope (optional)" section is NOT present in the dialog
    # Defensive default: useRbac() falls back to PERMISSIVE_SUMMARY
    # (isRbacEnabled=false). On a failed fetch we behave as if RBAC is off,
    # which is the safe choice for this UI: never offer a control that has
    # no effect.

  @in-memory @milestone-1
  Scenario: M1.4 Scope picker is hidden while the authorization summary is still loading
    Given the authorization summary request has been issued but has not yet resolved
    When the user clicks "New API Key"
    Then the Create API Key dialog is open
    And the "Restrict scope (optional)" section is NOT present in the dialog
    # While loading, useRbac().isRbacEnabled defaults to false (the
    # permissive summary). Showing the section only once we have a
    # confirmed isRbacEnabled=true avoids a flash of the disallowed
    # control followed by it disappearing.

  @in-memory @milestone-1
  Scenario: M1.5 Submitting without scopes (RBAC off) sends no scope field
    Given RBAC is disabled in the running deployment
    And the user has opened the Create API Key dialog
    When the user enters the name "CLI key"
    And the user submits the form
    Then the POST /api/apikeys request body does NOT contain a "scope" property
    And the create request returns "201 Created"
    # Today, an empty scopeRows array already results in request.scope
    # being omitted (ApiKeysSettings.tsx:144). This scenario pins that
    # behaviour so that the future fix does not accidentally introduce
    # an empty scope[] payload.
