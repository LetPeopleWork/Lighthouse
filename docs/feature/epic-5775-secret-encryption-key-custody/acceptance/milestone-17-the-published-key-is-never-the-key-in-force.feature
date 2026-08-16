Feature: The published key is never the key in force (Epic 5775, Slice 04b — US-04b)
  As the operator who kept their own configuration file across the upgrade
  I want Lighthouse to refuse to write secrets with the key that ships inside every copy of it
  So that I am never told an instance is healthy while its credentials are protected by a public value

  # Every check that existed before this slice reasons about key identity or envelope shape. Nothing
  # compared key material, so the published key supplied through configuration — wearing an id derived
  # from its own bytes — walked past the one check built to catch it, and the instance then reported
  # itself healthy. The comparison is over 32 compiled-in bytes and costs nothing.
  #
  # Refusing is the whole point, and it has to happen at the one moment it can still be said: before a
  # single credential has been written under that key. Everything about such an instance looks fine
  # afterwards.
  #
  # The refusal is about the key secrets would be *written* under. Behind an active key the same material
  # stays welcome and always will, because that is how an instance that upgrades keeps reading what it
  # already stored.

  @error @driving_port @us-04b @slice-04b
  Scenario: A supplied key that is the key published with the product stops the start
    Given the key published with Lighthouse is set as this instance's encryption key
    When Lighthouse starts
    Then it refuses to start
    And the refusal says that the key supplied is the key published with the product
    And it says what makes that key no protection at all
    And it says nothing already stored has been changed or lost

  @error @us-04b @slice-04b
  Scenario: The refusal names the setting the key arrived in
    Given the key published with Lighthouse is set as this instance's encryption key
    When Lighthouse starts
    Then the refusal names the setting that carried it
    And it names one thing to do instead

  @edge @error @us-04b @slice-04b
  Scenario: The name this release retired is refused on the same terms
    Given an operator who kept their own configuration across the upgrade, still carrying the pre-upgrade key setting
    And the value in it is the key published with Lighthouse
    When Lighthouse starts
    Then it refuses to start
    And the refusal names that setting rather than the one this release documents

  @edge @error @us-04b @slice-04b
  Scenario: A ring whose first entry is the published key is refused
    Given a ring of two keys is supplied, and the first of them is the key published with Lighthouse
    When Lighthouse starts
    Then it refuses to start
    And the refusal names the ring setting

  @edge @error @us-04b @slice-04b
  Scenario: A key mounted from an external store is refused on the same terms
    Given a key file mounted from outside holds the key published with Lighthouse
    When Lighthouse starts
    Then it refuses to start
    And the refusal names the file it was read from

  @edge @us-04b @slice-04b
  Scenario: The published key behind a key of the operator's own is welcome
    Given a ring of two keys is supplied, and the second of them is the key published with Lighthouse
    When Lighthouse starts
    Then it starts
    And secrets are written under the operator's own key
    And what was stored under the published key is still readable

  @real-io @us-04b @slice-04b
  Scenario: An instance that upgrades still reads everything it stored
    Given an instance holding credentials written by the version before this one
    And no encryption key is supplied to it
    When Lighthouse starts
    Then it starts
    And every one of those credentials is readable
    And nothing new is written under the key published with the product

  @property @error @us-04b @slice-04b
  Scenario: No refusal repeats a byte of the key it refused
    Given any of the ways a supplied key can be the key published with Lighthouse
    When Lighthouse refuses to start
    Then the refusal contains no part of the key material
    And the refusal is read from a console or a log that keeps it, which is why

  @edge @us-04b @slice-04b
  Scenario: An instance with nowhere to keep a key of its own is not caught by this
    Given an instance that cannot keep a key that would survive a restart
    And it already holds stored credentials
    When Lighthouse starts
    Then it starts on the key published with the product, as it did before
    And it says so, because that is a different problem with a different way out
