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
  Scenario: The published key can never become the key new secrets are written under
    Given any way an instance can arrive at its key ring
    When the ring is resolved
    Then the published key is only ever one the instance can read with
    And no configuration, no ordering and no rotation can make it the key that writes

  @regression @us-02 @slice-02 @upgrade-from-pre-epic
  Scenario: Upgrading moves no secret that was already stored
    Given an instance upgraded onto a key of its own, holding credentials written under the published key
    When the upgrade completes
    Then every one of those credentials is stored exactly as it was, still naming the published key
    And nothing walked the stored secrets during startup
    And the operator was not kept waiting on any such work before the instance was usable
