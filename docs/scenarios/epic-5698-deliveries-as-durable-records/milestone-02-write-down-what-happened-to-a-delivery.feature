Feature: Write down what happened to a Delivery, while it is still happening (Epic 5698, Slice 02 — US-02)
  As a Delivery Lead watching a Delivery's likelihood fall
  I want to write a short dated line against it
  So that in October somebody can still say whether it was a scope change or two people off sick

  # The Delivery already has a two-tab shell, and the second tab switches itself off until there are
  # three days of history behind it. The Notes tab must not inherit that: a note is worth writing on
  # the first day, and a tab that is dark on the day something notable happens is a tab nobody comes
  # back to.
  #
  # Attribution is the hard half. The instance may have authentication switched off, in which case
  # there is genuinely nobody to name. Two dishonest answers are available — refuse the note, or invent
  # a name — and both are rejected. The note is stored and shown with no author, which is the truth.
  # Every scenario below that involves an author therefore has a twin that involves none.
  #
  # The name shown on a note is the name captured when it was written, not the writer's name today.
  # A note is a dated record of what somebody said; re-labelling last quarter's note because a display
  # name changed is exactly the silent rewriting this whole Epic exists to stop. Two scenarios pin
  # that, because it is the kind of behaviour that looks like a bug to anyone who has not thought
  # about it and is quietly reverted.
  #
  # Three things DESIGN handed DISTILL to settle, settled here as acceptance criteria:
  #   * a note has no length limit, because no string column in this product has one and notes are
  #     not the place to introduce the first;
  #   * empty is refused in BOTH places, in the field and again at the API, because the API already
  #     cannot trust the browser for permissions and must not start trusting it for content;
  #   * notes come back newest-first, all of them, with no paging, and the order is stable across
  #     reads rather than "however the database felt".

  Background:
    Given a Portfolio holding a live Delivery

  @driving_adapter @us-02 @slice-02 @ac-02.1 @contract-shape:bounded-change
  Scenario: The Notes tab is there from the first day, unlike the one that waits for history
    When the lead expands the Delivery
    Then a third tab named Notes sits beside Work Items and Metrics
    And it can be opened even on a Delivery created moments ago
    And the Metrics tab beside it still waits for enough history, as it always has

  @driving_adapter @us-02 @slice-02 @ac-02.2 @ac-02.4 @contract-shape:bounded-change
  Scenario: A note written on a Delivery is listed against it, dated and signed
    Given the lead is signed in as "Anoop Kumar"
    When the lead opens the Notes tab and writes "Two Features added after the steering review"
    Then the note appears at the top of the list
    And it carries the day it was written
    And it is signed "Anoop Kumar"

  @us-02 @slice-02 @ac-02.2 @contract-shape:bounded-change
  Scenario: Notes read newest first, and read the same way every time
    Given four notes were written against the Delivery on four different days
    When the lead opens the Notes tab
    Then the most recently written note is first and the oldest is last
    When the lead leaves the tab and comes back
    Then the four notes are in exactly the same order
    And two notes written within the same second do not swap places between reads

  # Deferred deliberately: a Delivery accumulating hundreds of notes would want paging. Nobody has one.
  # The ceiling is stated so that the day someone does, the failure is a slow tab and not a broken one.
  @edge @us-02 @slice-02 @ac-02.2 @contract-shape:bounded-change
  Scenario: Every note on a Delivery comes back, without the reader asking for a second page
    Given the Delivery carries fifty notes
    When the lead opens the Notes tab
    Then all fifty are listed, newest first
    And there is no second page to fetch and nothing is silently dropped from the end

  @error @driving_adapter @us-02 @slice-02 @ac-02.3 @contract-shape:unbounded-preservation
  Scenario: A reader who may not change the Portfolio can read the notes but not add one
    Given a user who may read this Portfolio and not change it
    When that user opens the Notes tab
    Then the existing notes are listed in full
    And there is nowhere to type a new one
    When that user asks the product directly to add a note anyway
    Then the request is refused as beyond their rights
    And no note has been stored

  # The routes this slice adds are rooted at the Delivery, not at the Portfolio, so the declarative
  # permission check the Portfolio routes use cannot see a Portfolio to check against. An endpoint that
  # reached for it anyway would fall back to "any signed-in caller" and pass every test that only ever
  # signs one in.
  @error @architecture @us-02 @slice-02 @ac-02.3 @contract-shape:unbounded-preservation
  Scenario: A signed-in user with no rights over this Portfolio cannot reach its Delivery's notes
    Given a user who is signed in and has no rights over this Portfolio at all
    When that user asks for the Delivery's notes
    Then the request is refused as beyond their rights, not answered
    And the same holds when that user asks to add one
    And being signed in is not by itself enough to reach anything under a Delivery

  @architecture @us-02 @slice-02 @ac-02.3 @contract-shape:unbounded-preservation
  Scenario: Every way into a Delivery checks who is asking and what they may see
    Given every route the product offers that is addressed by a Delivery
    When each is examined
    Then each one establishes which Portfolio the Delivery belongs to and checks the caller against it
    And a route added later that forgets to is reported rather than passing quietly

  @error @edge @us-02 @slice-02 @ac-02.5 @contract-shape:bounded-change
  Scenario: On an instance with nobody signed in, a note is stored with no author rather than a made-up one
    Given the instance runs with authentication switched off
    When someone writes "Scope cut agreed with the sponsor" against the Delivery
    Then the note is stored and listed
    And it carries the day it was written
    And it shows no author at all — no name, no placeholder and no empty by-line that reads as a fault

  @us-02 @slice-02 @ac-02.4 @contract-shape:unbounded-preservation
  Scenario: A note keeps the name it was written under when its author is renamed
    Given "Anoop Kumar" wrote a note against the Delivery
    When that person's display name is later changed to "Anoop K."
    And the lead opens the Notes tab
    Then the note is still signed "Anoop Kumar"
    And no other note's signature changed either

  @edge @us-02 @slice-02 @ac-02.4 @contract-shape:bounded-change
  Scenario: A note outlives the person who wrote it leaving the instance
    Given "Anoop Kumar" wrote a note against the Delivery
    When that person is removed from the instance
    Then the note is still listed with its text and its date
    And it now shows no author, rather than disappearing with them

  @error @us-02 @slice-02 @ac-02.6 @contract-shape:unbounded-preservation
  Scenario: An empty note is refused in the field and refused again when asked for directly
    Given the lead has the Notes tab open
    When the lead tries to save a note containing nothing
    Then it is refused with a message on the field
    When the lead tries to save a note containing only spaces and a line break
    Then it is refused the same way
    When the product is asked directly to store a note of only whitespace
    Then it is refused with a reason naming the field
    And in none of the three cases has anything been stored

  @us-02 @slice-02 @ac-02.6 @contract-shape:bounded-change
  Scenario: Leading and trailing blank space is not part of what somebody wrote
    When the lead saves a note typed with spaces and a line break before and after the text
    Then the note is listed with the text alone
    And the stored note carries no leading or trailing blank space

  @error @us-02 @slice-02 @ac-02.7 @contract-shape:unbounded-preservation
  Scenario: A note that goes to the wrong Delivery does not appear on the right one
    Given a second Delivery in the same Portfolio
    When a note is written against the first Delivery
    Then it is listed against the first Delivery
    And it is absent from the second Delivery's notes
    And it is absent from every Delivery in every other Portfolio

  @us-02 @slice-02 @ac-02.8 @contract-shape:bounded-change
  Scenario: Deleting a Delivery takes its notes with it
    Given three notes were written against the Delivery
    When the Delivery is deleted outright
    Then its three notes are gone
    And no other Delivery lost a note

  # Plain text, decided in DISCUSS and pinned here because the failure mode is a security one, not a
  # cosmetic one.
  @error @us-02 @slice-02 @ac-02.9 @contract-shape:pure-function
  Scenario: A note that looks like markup is shown as the characters somebody typed
    When the lead saves a note reading "Scope <b>doubled</b> after the review — see **the deck**"
    Then the note is displayed with the angle brackets, the asterisks and the words between them
      exactly as typed
    And nothing in it is bold, and nothing in it has become a link or a piece of the page

  @real-io @adapter-integration @us-02 @slice-02 @migration @contract-shape:bounded-change
  Scenario: An instance upgraded to the release that brings notes keeps everything it already had
    Given an instance running the previous release, carrying real Portfolios, Deliveries and weeks of
      recorded history
    When it is upgraded to the release that brings notes, on each kind of storage the product supports
    Then every Portfolio, Delivery and day of recorded history that was there is still there and reads
      exactly as it did
    And every Delivery is ready to take its first note
    And nothing that already existed was removed, renamed or rewritten to make room
