Feature: An install that names no key is refused (Epic 5775, Slice 05 — US-05)
  As an operator installing Lighthouse into a cluster, for my own organisation or as one tenant among many
  I want an install that supplies no key to stop before it creates anything
  So that no install can quietly come up on a key that everybody who downloaded the product already has

  # The chart provisions a database connection string and a sign-in secret and no encryption key at
  # all, so every cluster install — including every tenant on the platform — comes up on the published
  # default. The obvious fix is for the chart to make a key when none is supplied, and it is the wrong
  # one: the only mechanism available for "make one the first time" asks the cluster what is already
  # there, and that question comes back empty every time the manifests are rendered without a cluster
  # to ask. That is how the platform renders every tenant, on every sync. A chart that generates would
  # therefore mint a fresh key on each sync and orphan the tenant's entire credential set.
  #
  # So the chart makes nothing, and an install that names no key is refused. That is this chart's own
  # existing habit rather than a new rule — the database password is already required with no default,
  # for the same reason: a security-relevant value the chart invents is a value nobody owns. Refusing
  # removes the catastrophe by construction, and it removes it under the platform's own rendering path
  # too, which no cluster-asking guard ever could have.

  @driving_port @us-05 @slice-05 @error
  Scenario: An install supplying no key at all is refused
    Given an operator installing Lighthouse into a cluster
    When they supply neither a key of their own nor a secret that already holds one
    Then the install is refused before anything is created
    And the refusal names both ways forward
    And it carries a command that makes a key, so the way out is one paste rather than a search

  @driving_port @us-05 @slice-05 @error
  Scenario: The refusal happens where there is no cluster to ask
    Given manifests rendered without any cluster to consult, which is how the platform renders a tenant
    When no key and no secret is named
    Then the refusal is the same refusal
    And it is the same refusal because nothing in the chart ever asks a cluster what already exists

  @driving_port @us-05 @slice-05 @error
  Scenario: Naming both a key and a secret that holds one is refused
    Given an operator who supplies a key of their own and also names a secret that holds one
    When the install is rendered
    Then it is refused, saying it cannot be both
    And it is refused rather than resolved, because which of the two wins is not the chart's to decide

  @us-05 @slice-05 @property
  Scenario: Nothing in the chart can make a key
    Given every template the chart carries
    When they are searched for the ways a chart can make up a value — a random string, random bytes, a fresh identifier, a generated private key, or a question to the cluster about what already exists
    Then none of them appears anywhere near an encryption value
    And this is worth asking separately from whether two renders agree, because a generator guarded by a question to the cluster renders the same empty answer every time and still mints a key on a real install

  @us-05 @slice-05 @property
  Scenario: The same values rendered twice are the same manifests
    Given one set of install values naming a key
    When the manifests are rendered from them twice
    Then the two renders are identical, byte for byte
    And an upgrade that changes nothing therefore changes nothing

  @us-05 @slice-05 @edge
  Scenario: An operator who owns their other secrets elsewhere is still asked for this one
    Given an install whose database credential and sign-in secret are both owned by an external store
    When it names no encryption key and no secret that holds one
    Then it is still refused
    And it is refused because the question is asked of the install, not of a secret the chart may not be rendering at all

  @us-05 @slice-05 @edge
  Scenario: The single-container product is untouched
    Given the standalone product, which is one container and one file
    When this slice ships
    Then it starts exactly as it did, asking for nothing new
    And it never looks for a mounted file, because it was never told about one
