Feature: One address for "how do you look after my credentials?" (Epic 5775, Slice 06 — US-06, US-08)
  As someone who has just been told a credential is encrypted before it is saved
  I want the link beside that sentence to land on a page written for me
  So that I get the plain answer, and the security person I forward it to gets the proof, from one URL

  # Driving adapter = the secret-handling notice on the connection form, shipped in slice 01.
  # Slice 01 pointed its link at an anchor inside the configuration reference, because that was the only
  # place the facts existed. Slice 06 creates the canonical Security page and repoints the link at it.
  # Everything else this slice owes is prose in published documents, which no test can assert without
  # pinning a phrasing instead of a behaviour; those criteria are carried as a named-file checklist in
  # the DELIVER handoff rather than as scenarios here.
  # This runs as a frontend Vitest test, extending SecretHandlingNotice.test.tsx.

  @driving_adapter @us-06 @slice-06
  Scenario: The notice sends someone to the page that answers the question it raises
    Given the secret-handling notice is shown on a connection form
    When someone follows its link
    Then they arrive at the canonical page about how Lighthouse protects credentials
    And not at a section inside the configuration reference, which is written for whoever installs Lighthouse rather than for whoever is pasting the credential

  @edge @us-06 @slice-06
  Scenario: The link is one address, whoever it is forwarded to
    Given the secret-handling notice is shown on a connection form
    When someone copies its link and sends it to a security reviewer
    Then the reviewer opens the same address the person pasting the credential opened
    And nothing in the address depends on which work tracking system the form was for
