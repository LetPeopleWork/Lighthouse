Feature: API key scope picker is hidden when RBAC is disabled
  As a Lighthouse user creating an API key on a deployment where RBAC is
  disabled
  I want the "Restrict scope (optional)" section to be absent from the
  Create API Key dialog
  So that I am not invited to configure a per-key scope that the backend
  will silently ignore

  Background:
    Given Lighthouse is running with authentication enabled
    And the authenticated user is allowed to open the API Keys settings tab

  @walking_skeleton @in-memory @driving_adapter
  Scenario: Non-admin opens Create API Key dialog on an RBAC-disabled deployment and sees no scope picker
    Given RBAC is disabled in the running deployment
    And Jordan is signed in as an authenticated user with no System Admin role
    When Jordan opens the "API Keys" settings tab
    And Jordan clicks "New API Key"
    Then the Create API Key dialog is open
    And the Name and Description fields are present
    And the "Restrict scope (optional)" section is NOT present in the dialog
    When Jordan enters the name "Jordan CLI" and submits the form
    Then the create request is sent with no "scope" field in its body
    And the create request succeeds
    And the plaintext key is shown exactly once
