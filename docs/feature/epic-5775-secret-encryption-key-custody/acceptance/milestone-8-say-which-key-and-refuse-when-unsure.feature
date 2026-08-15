Feature: Say which key this instance is on, and refuse rather than guess (Epic 5775, Slice 02 — US-02)
  As an operator responsible for an instance holding other people's tokens
  I want to be able to answer "which key am I on, and is my data safe if the file leaks?"
  So that I know whether there is anything left for me to do

  # Two halves. The first is disclosure: the key source, the name of the active key and the resolved key
  # store location are stated on the startup line and behind a System-Admin-guarded surface — never the
  # key itself, and never on the system information endpoint, whose audience includes anyone who reaches
  # an embedded frame (DESIGN F-4, ADR-137). The second half is refusal: every ambiguity stops the boot
  # instead of being guessed at, because generating a fresh key on an existing database looks like a
  # successful start while orphaning every secret in it (D10, ADR-149).
  # These run as WebApplicationFactory integration tests, backend NUnit bootstrap tests, and Vitest
  # tests over the Settings page reading the guarded surface.

  @driving_adapter @us-02 @slice-02
  Scenario: The startup line says where the key came from, what it is called, and where it is kept
    Given an instance that has resolved its key ring
    When it starts
    Then one line states where the key came from, the name of the active key and the resolved key store location
    And that line contains no part of the key itself

  @real-io @driving_adapter @us-02 @slice-02
  Scenario: The System settings page shows the same, and only a System Administrator can see it
    Given a System Administrator looking at the System settings page
    When the page loads
    Then it shows where the key came from and the name of the active key
    And it obtained them from the surface only System Administrators can reach

  @error @driving_adapter @us-02 @slice-02
  Scenario: Someone who is not a System Administrator learns nothing about the key
    Given a person signed in without System Administrator rights
    And a viewer reaching the instance through an embedded frame
    When either asks the instance about its encryption
    Then each is refused
    And neither learns the key source, the active key name, or where the key is kept

  @regression @driving_adapter @us-02 @slice-02
  Scenario: The system information surface says nothing about keys that it did not say before
    Given the system information an embedded viewer can already reach
    When it is read on this version
    Then it carries exactly what it carried before this slice
    And nothing in it names a key, a key source, or a key store location

  @error @us-02 @slice-02
  Scenario: A key store that exists and cannot be read stops the instance rather than starting over
    Given an instance whose stored key ring is present but cannot be read
    When it starts
    Then it refuses to start and names the key store
    And no replacement key is written
    And the secrets in its database are left exactly as they are

  @edge @us-02 @slice-02 @docker-no-data-volume
  Scenario: An existing instance with nowhere durable to keep a key keeps working, and says so
    Given an instance already holding encrypted credentials
    And no location this instance can promise to still have tomorrow
    When it starts
    Then it starts and every stored credential keeps working
    And it says plainly that it is still on the key published with the product
    And it names the two things the operator can do about it

  @error @us-02 @slice-02 @docker-no-data-volume
  Scenario: A fresh instance with nowhere durable to keep a key refuses to start
    Given an instance with no stored credentials at all
    And no location this instance can promise to still have tomorrow
    When it starts
    Then it refuses to start
    And it names the two things the operator can do about it
    And it does not quietly begin on the key published with the product

  @real-io @driving_adapter @us-02 @slice-02
  Scenario: A System Administrator can see who owns the key and whether this instance may make a new one
    Given a System Administrator asking the instance about its encryption
    When the answer comes back
    Then it says where the key came from, which key is active, which keys are held, and where they are kept
    And it says whether this instance is able to make a new key at all
    And it says whether the key published with the product is still one of the keys held
    And it carries no key material of any kind
