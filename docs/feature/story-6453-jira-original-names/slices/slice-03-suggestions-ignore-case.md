# Slice 03 — Suggestions list each name once

**Goal:** state and work item type suggestions are de-duplicated ignoring case.

**IN:** `SuggestionsController` Teams/Portfolios states and types (eight `Distinct()` calls), case-insensitive, first
spelling kept. Its job is the Jira case of a connector-neutral change.

**OUT:** merging German and English spellings (MD-2/MD-3: no translation logic).

**Learning hypothesis:** confirms the doubled entries in the report were a case clash, not a language clash; if the
customer's list still shows doubles after 01 and 03, they are leftovers of German configuration (MD-3).

**Acceptance criteria:** AC-3.1, AC-3.2.

**Dependencies:** none. Can be dropped without harm.

**Effort:** ≤2 hours.
