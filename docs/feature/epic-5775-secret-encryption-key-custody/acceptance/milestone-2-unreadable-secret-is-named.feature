Feature: An unreadable secret is named on its own connection and never leaves the instance (Epic 5775, Slice 01 — US-01)
  As a platform operator
  I want an unreadable secret to be reported against the connection and the field that holds it
  So that I know whether to go fix the key or go re-issue the token — two very different afternoons

  # Driving adapters = the Connections settings UI (the field-level state an operator reads) and the
  # existing connection-validation action. Driving port behind them = the connection list read model,
  # which gains a per-secret readability state derived on read.
  # Mechanism behind the steps: the decrypt-returns-ciphertext fallback is deleted; reading an
  # unverifiable secret raises, and the four credential styles plus the token refresh path let it
  # travel rather than handling it (ADR-147). The once-per-secret log de-duplication lives in the
  # crypto service, keyed on a hash of the stored value, so no caller changes.
  # These run as backend NUnit tests (unit for the reader, WebApplicationFactory for the read model
  # and the validation action) plus one frontend Vitest test for the field-level rendering.

  @real-io @driving_adapter @us-01 @slice-01
  Scenario: A connection holding a secret that cannot be read shows that state on the field that holds it
    Given a connection whose stored credential can no longer be read
    When an administrator opens that connection
    Then the field holding that credential is marked as unreadable with the encryption key named as the cause
    And the other fields on the connection are unaffected

  @regression @us-01 @slice-01
  Scenario: A connection whose secrets all read shows no unreadable state anywhere
    Given a connection whose stored credentials can all be read
    When an administrator opens that connection
    Then no field is marked as unreadable
    And the connection looks exactly as it does today

  @error @real-io @us-01 @slice-01
  Scenario Outline: No work tracking system is ever handed a credential the instance could not read
    Given a connection using <credential style> whose stored credential can no longer be read
    When the instance prepares to talk to that work tracking system
    Then it stops before sending anything
    And the outgoing request carries no credential at all

    Examples:
      | credential style          |
      | a personal access token   |
      | a hosted API token        |
      | an API key                |
      | a service account password|

  @error @real-io @us-01 @slice-01
  Scenario: A token refresh stops rather than sending a refresh token the instance could not read
    Given a connection authorised earlier whose stored refresh token can no longer be read
    When the instance tries to renew that authorisation
    Then it stops before sending anything
    And the operator is told the stored token cannot be read rather than that the authorisation expired

  @error @us-01 @slice-01
  Scenario: An unreadable secret is reported once, not once for every attempt to use it
    Given a connection whose stored credential can no longer be read
    When that credential is used many times over the life of the instance
    Then the operator sees the problem reported once for that secret
    And repeating the attempt does not repeat the report

  @error @us-01 @slice-01
  Scenario: Nothing about the secret or the key itself is written down when the failure is reported
    Given a connection whose stored credential can no longer be read
    When the failure is reported
    Then the report names only which state the secret is in and which key it claims to have been written with
    And it contains no part of the key, the stored value, or the credential

  @error @driving_adapter @us-01 @slice-01
  Scenario: Validating a connection with an unreadable secret reports a key problem, not a rejected credential
    Given a connection whose stored credential can no longer be read
    When an administrator asks Lighthouse to validate that connection
    Then the result says the secret cannot be read with the current encryption key
    And it does not say the work tracking system rejected the credential
