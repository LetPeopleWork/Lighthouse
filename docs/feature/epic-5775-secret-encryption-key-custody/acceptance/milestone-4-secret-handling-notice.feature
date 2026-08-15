Feature: Knowing a credential is safe to paste, before pasting it (Epic 5775, Slice 01 — US-08)
  As someone about to paste a personal access token into a tool I did not write
  I want the form to tell me plainly what happens to it
  So that I can proceed without stopping to ask my security team — and so that I have something to send
  them if I decide to ask anyway

  # Driving adapter = the connection form in the Connections settings UI.
  # One generic notice per form, rendered once where the form contains at least one secret field. It
  # names no connector and no algorithm, so a single string serves every secret any connector defines
  # now or later, and every claim in it is already true on every install regardless of which key the
  # instance holds — which is why this ships in slice 01 rather than waiting for key custody.
  # These run as frontend Vitest + React Testing Library tests, plus the reload check which is already
  # asserted on the backend by the connection list payload shape tests.

  @driving_adapter @us-08 @slice-01
  Scenario: Someone pasting a credential is told, once on the form, what happens to it
    Given a connection form that asks for at least one credential
    When someone opens that form
    Then they see one notice saying the credential is encrypted before it is saved, is never shown again to anyone, never leaves this instance, and can be revoked wherever it was created
    And the notice offers a link explaining how Lighthouse protects credentials

  @edge @us-08 @slice-01
  Scenario: A form that asks for no credential shows no notice
    Given a connection form with no secret field on it
    When someone opens that form
    Then no secret-handling notice is shown

  @edge @us-08 @slice-01
  Scenario: A form asking for several credentials still shows exactly one notice
    Given a connection form that asks for more than one credential
    When someone opens that form
    Then exactly one secret-handling notice is shown
    And it is not repeated beside each field

  @edge @us-08 @slice-01
  Scenario: The same notice serves every kind of connection
    Given the connection forms for every work tracking system Lighthouse supports
    When someone opens each of them in turn
    Then each shows the same notice, word for word
    And the notice names no particular work tracking system and no particular way of protecting the credential

  @regression @us-08 @slice-01
  Scenario: The one claim a person can check in four seconds holds
    Given a connection that was saved with a credential
    When someone reopens that connection
    Then the credential field is blank
    And the credential is not sent to the browser for anyone, whatever they are allowed to do

  @edge @us-08 @slice-01
  Scenario: The notice never ships with a link that goes nowhere
    Given the notice is shown on a connection form
    When someone follows its link
    Then they arrive at the current published explanation of how Lighthouse handles credentials

  @edge @us-08 @slice-01
  Scenario: The notice answers a question rather than raising an alarm
    Given the notice is shown on a connection form
    When someone reads it
    Then it is presented as information and not as a warning
    And it makes no claim about which encryption key this instance holds
    And it makes no claim of protection against someone who already holds the key or the host
