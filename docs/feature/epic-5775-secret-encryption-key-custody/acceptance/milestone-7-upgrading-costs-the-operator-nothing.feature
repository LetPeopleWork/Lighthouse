Feature: Upgrading onto an instance's own key costs the operator nothing (Epic 5775, Slice 02 — US-02)
  As an operator upgrading an instance whose secrets were all written under the published key
  I want every credential I already stored to keep working
  So that the day I upgrade is not the day I re-enter every token I have

  # The published key stops being a setting and becomes a key the instance can only ever read with.
  # That is what lets the literal leave the shipped settings file while every secret written under it
  # stays readable (ADR-148): the two facts now live in different places. Nothing is re-encrypted by the
  # upgrade — that is an operator-triggered action in a later slice (D13). What this slice owes is that
  # the upgrade genuinely leaves stored secrets alone; saying so unprompted, and counting what is still
  # on the published key, belongs to the encryption surface in slice 04 and the docs in slice 06.
  # These run as backend NUnit tests plus one integration pass against the development instance
  # restored from a real backup, because demo secrets were written by the same build that reads them.

  @real-io @driving_port @us-02 @slice-02 @upgrade-from-pre-epic
  Scenario: An instance upgrading from the published key reads every secret it already had
    Given an instance whose stored credentials were all written under the published key
    When it is upgraded to this version and started
    Then every one of those credentials is still readable
    And every connection still syncs without anyone touching it
    And nobody is asked to re-enter anything

  @edge @us-02 @slice-02 @upgrade-from-pre-epic
  Scenario: The published key is gone from the shipped settings and the upgrade still works
    Given the shipped settings file of this version
    When it is inspected for key material
    Then it holds none
    And an instance upgrading onto this version still reads the secrets it wrote under the published key

  @real-io @driving_port @us-02 @slice-02 @upgrade-from-pre-epic
  Scenario: After the upgrade, a newly saved credential is written under this instance's own key
    Given an upgraded instance that has resolved a key of its own
    When a credential is saved
    Then the stored secret names the instance's own key, not the published one
    And the credentials stored before the upgrade are untouched

  @property @edge @us-02 @slice-02
  Scenario: Wherever an instance has a key of its own, the published key only ever reads
    Given an instance that resolved a key of its own, by any of the ways it can
    When the ring is resolved
    Then the published key is present only as one the instance can read with
    And nothing about how the ring was assembled can promote it to the key that writes

  @edge @us-02 @slice-02 @docker-no-data-volume
  Scenario: An instance with no key of its own keeps writing under the published key, and is told so
    Given an instance with nowhere durable to keep a key, already holding credentials
    When the ring is resolved
    Then the published key is the only key it has, so new secrets are written under it too
    And this is what the instance did before this change, not something this change introduced
    And the operator is told, which is what this change adds

  @regression @us-02 @slice-02 @upgrade-from-pre-epic
  Scenario: Upgrading moves no secret that was already stored
    Given an instance upgraded onto a key of its own, holding credentials written under the published key
    When the upgrade completes
    Then every one of those credentials is stored exactly as it was, still naming the published key
    And nothing walked the stored secrets during startup
    And the operator was not kept waiting on any such work before the instance was usable
