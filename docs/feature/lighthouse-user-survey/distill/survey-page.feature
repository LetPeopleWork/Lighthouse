@feature:lighthouse-user-survey @US-01 @US-03
Feature: A stable, shareable survey page whose link survives question edits

  A community member reaches a clean survey page at one stable address — whether
  from a shared link or the in-app nudge — with no login, no account, and no name
  asked. The maintainer can change the questions later as content, and every
  previously-shared link keeps working and shows the current questions. When the
  question content cannot be loaded the page stays graceful rather than blank.
  The page is reachable only by its address; it is not advertised anywhere.

  @driving_port @in-memory @US-01 @slice-01 @pending
  Scenario: A community member opens the survey page and sees the current questions
    Given the survey page is available
    When a community member opens the survey page
    Then the current set of questions is shown
    And no login or account is required

  @driving_port @in-memory @US-01 @slice-01 @pending
  Scenario: The nudge link and a shared link reach the same survey page
    Given a community member arrives from the in-app nudge
    And another community member arrives from a link shared on a chat channel
    When each of them opens the survey page
    Then both reach exactly the same survey page at the same address

  @driving_port @in-memory @US-03 @slice-02 @pending
  Scenario: The maintainer changes the questions without changing the link
    Given a community member has a previously-shared link to the survey page
    When the maintainer changes the survey questions
    Then the previously-shared link still resolves to the survey page
    And the survey page shows the updated questions
    And the address of the survey page did not change

  @driving_port @in-memory @US-03 @slice-02 @pending
  Scenario: Old and new responses stay readable after the questions change
    Given responses were collected under an earlier set of questions
    When the maintainer changes the questions and new responses are collected
    Then both the earlier and the newer responses remain readable in the survey view

  @error @driving_port @in-memory @US-01 @slice-01 @pending
  Scenario: The page stays graceful when the question content cannot be loaded
    Given the survey question content cannot be loaded
    When a community member opens the survey page
    Then the community member sees a "survey temporarily unavailable" message
    And the community member does not see a blank page

  @driving_port @in-memory @US-01 @slice-01 @pending
  Scenario: The survey page is reachable by its address but is not advertised
    Given the website is published
    When a visitor browses the website navigation and the site map
    Then the survey page is not listed anywhere
    And a community member who opens the survey address directly still reaches the page
