# Slice 02 — One full refresh of every Jira Team and Portfolio after the upgrade

(Number re-used: the earlier slice 02, history by status IDs, was dropped under MD-7.)

**Goal:** items stored with translated states by a German-token account, under a configuration written in English, are
re-read once after the upgrade instead of staying wrong until each one changes.

**IN:** a Jira reading-version marker in `FetchFingerprint.ForConnection`, rendered only for Jira connections (DD-11,
MD-15). Every non-Jira digest stays byte-identical, with a test that pins it. The Jira pinned digests
(`FetchFingerprintPinnedDigestTest`, `FetchFingerprintAcrossTheConnectionDefaultsUpgradeTest`) move on purpose and their
rationale text says why. Its tests live in `JiraReadingVersionFingerprintTest`. Guard test `FetchShapingPropertyGuardTest`
stays green.

**OUT:** migrating stored data (MD-3); any other connector; Teams configured in German (they still need Load from board
again).

**Separately droppable:** its own commit after slice 01 is green. Dropping it leaves slice 01 correct for new and
changed items; the release note then says to save the Team once (or re-load from board) to force a full refresh.

**Accepted cost:** every Jira Team and Portfolio runs one full cycle once, and that refresh's log carries the reason
`configuration-changed`, which is untrue for an upgrade (MD-15).

**Learning hypothesis:** confirms that a Team with an old fingerprint takes one full refresh and re-reads its states,
and that the second refresh is a delta again.

**Story and acceptance criteria:** US-05, AC-1.12 in `feature-delta.md` (the first refresh after the upgrade is a
full one and the Team's items show their original states with no admin action). DD-11 and its tests are the contract.

**Dependencies:** slice 01.

**Effort:** about half a day, most of it re-pinning the two digest tests.
