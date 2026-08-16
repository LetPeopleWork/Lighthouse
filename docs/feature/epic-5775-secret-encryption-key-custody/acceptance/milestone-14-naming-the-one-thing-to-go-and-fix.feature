Feature: Naming the one thing to go and fix (Epic 5775, Slice 04 — US-04)
  As an administrator who has just been told a credential cannot be read
  I want to be told which Connection and which field it is
  So that I can go and retype that one thing instead of searching for it

  # This is the slice's learning hypothesis, and the whole reason the check is not the rotation report
  # with a flag flipped. An operator told "two secrets are unreadable" has been handed a search. An
  # operator told "Jira Production · Personal access token" has been handed an answer they can act on in
  # a minute. Everything else in the check exists to make that sentence trustworthy.
  # Nothing in what comes back is a credential. The report travels to a browser and into whatever the
  # browser's console keeps, so a stored value or a decrypted one appearing anywhere in it would move
  # every secret in the installation somewhere nobody is guarding.

  @error @us-04 @slice-04
  Scenario: An unreadable secret is named by the Connection and the field that own it
    Given a Connection named for its work tracking system holding a credential no held key can read
    When an administrator checks the stored secrets
    Then the unreadable secret is listed with that Connection's name
    And with the name of the field that holds it
    And an administrator reading it knows which single thing to retype

  @edge @us-04 @slice-04
  Scenario: A Connection with nothing wrong is not asked to be looked at
    Given one Connection whose secrets are all readable and one holding an unreadable secret
    When an administrator checks the stored secrets
    Then only the second Connection is named as needing attention
    And the first is still counted among the secrets that were checked

  @error @us-04 @slice-04
  Scenario: What comes back carries no credential of any kind
    Given an instance holding readable secrets and unreadable ones
    When an administrator checks the stored secrets
    Then nothing in what comes back is a stored value
    And nothing in it is a decrypted credential
    And what is named is the Connection, the field and the key, which is everything needed to act and nothing more

  @edge @us-04 @slice-04
  Scenario: An instance holding no stored secrets says so rather than saying nothing
    Given an instance with no stored secrets at all
    When an administrator checks the stored secrets
    Then the check reports zero secrets and names the key in force
    And it does not read as a failure, because having nothing stored is a perfectly ordinary state
