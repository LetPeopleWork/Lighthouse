Feature: A key this instance generated, kept where the instance's data is kept (Epic 5775, Slice 02 — US-02)
  As a self-hoster who downloaded the exe or ran one docker run
  I want my instance to protect my credentials with a key that belongs to it alone
  So that a copy of my database is worth nothing to whoever takes it, without my having read anything first

  # Driving port = application bootstrap. The ring is resolved before the application is built, so a key
  # problem is a startup failure an operator can act on rather than a crash on the first credential save
  # (ADR-150). The key store is resolved by one function with four ordered cases, and the case that
  # matters here is the one that derives the directory from the database file, so an operator who
  # already mounted a data volume keeps their key by doing nothing (ADR-149).
  # These run as backend NUnit tests: a table test over the resolver's cases, WebApplicationFactory
  # boots for the ones that need a real bootstrap, and one manual container-recreation gold test that
  # is the only evidence that the mounted volume actually keeps the key.

  @real-io @driving_port @us-02 @slice-02
  Scenario: A first start with nothing supplied gives the instance a key of its own
    Given an instance starting for the first time with no key supplied by the operator
    When it starts
    Then it generates a key for itself and keeps it where only this instance can read it
    And the key it generated is the one new secrets are written under
    And nothing about the key itself is written to the log or to any configuration the instance can enumerate

  @real-io @driving_port @us-02 @slice-02 @regression
  Scenario: Restarting is not a rotation
    Given an instance that generated its own key on a previous start and has saved a credential since
    When it is stopped and started again
    Then it resolves the same key it generated before
    And the credential saved earlier reads back unchanged

  @real-io @driving_adapter @us-02 @slice-02
  Scenario: The key is kept beside the data, and the startup line says exactly where
    Given an instance whose database lives in a directory the operator chose
    When it starts
    Then the key it generates is kept in that same directory
    And the startup line names the resolved location, so the operator can see what they would have to back up

  @real-io @adapter-integration @us-02 @slice-02 @docker-with-data-volume
  Scenario: Replacing the container against the same mounted data directory keeps every secret readable
    Given a container whose data directory is a mounted volume, holding a database with saved credentials
    When the container is destroyed and a new one is created against the same volume
    Then the new container resolves the same key as the old one
    And every stored secret is still readable, with nothing re-entered

  @edge @us-02 @slice-02 @upgrade-from-pre-epic
  Scenario: An instance whose key was kept in the old place keeps it, rather than starting over
    Given an instance whose key store is still in the location earlier versions used
    And nothing has been written to the location this version resolves
    When it starts
    Then the existing key is carried across to the resolved location, once
    And the startup line names both locations so the move is visible rather than silent

  @error @us-02 @slice-02
  Scenario: Two key stores that disagree stop startup rather than one of them winning
    Given both the old and the newly resolved locations hold a key, and the two are not the same
    When the instance starts
    Then it refuses to start and names both locations
    And it neither picks one nor merges them

  @error @us-02 @slice-02
  Scenario: A key that cannot be read back the moment after it was written is not accepted
    Given a first start on a filesystem that accepts a write and does not durably keep it
    When the instance generates its key and immediately reads it back to check
    Then the mismatch stops startup
    And the instance does not carry on as though it had a key it will still have tomorrow

  @regression @us-02 @slice-02 @standalone-exe
  Scenario: The standalone application is unchanged by this slice
    Given the standalone application, which already keeps its key store beside its database
    When it starts on this version
    Then its key store resolves to the same directory it resolved to before
    And an existing standalone install reads every secret it already had

  @edge @us-02 @slice-02
  Scenario Outline: Where the key store lands is decided by one rule, applied in a stated order
    Given an instance configured with <configuration>
    When the key store location is resolved
    Then it resolves to <location>

    Examples:
      | configuration                                               | location                                     |
      | an explicit key store path                                  | the path the operator named                  |
      | no key store path, and a database file at an absolute path  | the directory that database file lives in    |
      | no key store path, and a database named by a bare filename  | the default location, and minting is refused |
      | no key store path, and a database that is not a file at all | the default location, and minting is refused |
