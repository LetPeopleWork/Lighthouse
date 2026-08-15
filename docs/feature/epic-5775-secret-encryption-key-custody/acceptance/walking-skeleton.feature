Feature: A stored credential that cannot be read says so (walking skeleton — Epic 5775, Slice 01)
  As a platform operator
  I want a stored credential that can no longer be read to be named on the connection that holds it
  So that a key problem looks like a key problem instead of arriving days later as a work tracking
  system rejecting my token

  # Walking skeleton: proves the whole backbone of slice 01 in one thin vertical —
  # save a credential -> it is stored in the authenticated self-describing format -> the connection
  # works -> damage the stored value -> the connection names the field, the work tracking system is
  # never called, and the refresh does not claim success.
  # Driving adapter = the Connections settings UI through the production composition root
  # (Playwright + Page Object Model against a running instance with demo data).
  # Mechanism behind the steps: AES-GCM envelope LH1.<keyId>.<nonce>.<ciphertext||tag> (ADR-146) and
  # the four-state reader that replaces the decrypt-returns-ciphertext fallback (ADR-147).

  @walking_skeleton @real-io @driving_adapter @us-01 @us-07 @slice-01
  Scenario: An administrator saves a credential, uses it, and is told plainly when it stops being readable
    Given Lighthouse is running with the demo data loaded
    When the administrator opens Settings and creates a connection to a work tracking system
    And the administrator pastes a valid credential into the connection's secret field and saves it
    Then the connection reports that it is working
    And the stored credential names the key it was written with and proves it has not been altered
    When the stored credential is altered so that it can no longer be read
    And the administrator reopens the connection
    Then the connection shows that the secret cannot be read with the current encryption key, against the field that holds it
    And the connection reports a problem with the encryption key rather than a rejected credential
    And no request carrying that credential is sent to the work tracking system
