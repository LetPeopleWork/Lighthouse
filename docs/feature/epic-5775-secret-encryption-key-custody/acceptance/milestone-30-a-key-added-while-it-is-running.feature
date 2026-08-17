Feature: A key added while it is running (Epic 5775, Slice 05 — US-05)
  As an operator rotating the key of a running instance from my own secret store
  I want a key I add to be picked up without restarting anything, and a bad edit to change nothing
  So that a rotation is four small steps I can watch land, rather than one step I have to get right

  # The rotation this makes possible is entirely the operator's: add the new key alongside the old,
  # watch the instance say it picked them up, ask it to re-encrypt onto the new one, then drop the old.
  # Lighthouse writes to no secret at any point and holds no permission to. Every step is visible
  # between the ones either side of it, which is what makes a rotation something an operator can stop
  # halfway through rather than a single irreversible action.
  #
  # The pickup is a re-read on a timer rather than a subscription to the file changing. A cluster
  # replaces a mounted secret by writing a new directory and moving a link, so a subscription to the
  # old file is a subscription to something that no longer exists and never fires again. A re-read
  # cannot be defeated by that, and half a minute is well inside the round trip of editing a secret and
  # going to look at the panel.
  #
  # Both guards keep the keys already in force. The ring that is running is known to work; nothing
  # arriving in a file is known to work until it has been read, so a file that will not read leaves
  # what is running exactly where it is.

  @driving_port @us-05 @slice-05 @real-io
  Scenario: A key added to the file is picked up without a restart
    Given a running instance reading its keys from a mounted file
    When an operator adds a new key ahead of the old one
    Then the instance is on the new key shortly afterwards, without anything being restarted
    And it says so, naming the keys it now holds

  @driving_port @us-05 @slice-05 @real-io
  Scenario: The pickup survives the way a cluster replaces a mounted file
    Given a running instance whose mounted file is reached through a link, as a cluster projects it
    When the file is replaced the way a cluster replaces it, by moving the link rather than rewriting the file
    Then the instance still notices, on the next read
    And it notices because it re-reads the content rather than waiting to be told the file changed

  @error @us-05 @slice-05
  Scenario: A file that cannot be read leaves the keys in force alone
    Given a running instance with a working set of keys
    When the mounted file is replaced with something that is not keys
    Then the keys in force stay in force and every stored credential still opens
    And the instance says the file could not be read, and why
    And it says it loudly, because a rotation an operator believes landed and did not is worse than a refusal

  @error @us-05 @slice-05
  Scenario: A file that reads but holds no keys is refused the same way
    Given a running instance with a working set of keys
    When the mounted file is replaced with one holding nothing at all
    Then the keys in force stay in force
    And the instance refuses the change rather than coming up keyless

  @us-05 @slice-05 @edge
  Scenario: Removing a key is accepted, and said out loud
    Given a running instance holding a new key and a retired one
    When the operator removes the retired key
    Then the instance accepts it, because custody is the operator's and not its own to argue with
    And it warns, naming the key that went away

  @error @us-05 @slice-05
  Scenario: Credentials on a key that was removed report unreadable rather than being tried anyway
    Given an operator who dropped the old key before asking for the re-encryption
    When the instance next reads a credential stored under it
    Then that credential is reported as unreadable, on the connection and the field that holds it
    And nothing is sent to the work tracking system with it, so the failure is one message rather than a loop of rejections

  @property @us-05 @slice-05
  Scenario: Nothing said about a reload is key material
    Given every message the instance writes about picking keys up, accepting them or refusing them
    When they are read in any encoding
    Then none of them is key material
    And the ones that name a key name only its identity, which is what an operator matches against their own store

  @property @us-05 @slice-05
  Scenario: Re-reading the same content changes nothing
    Given a running instance whose mounted file has not been edited
    When it re-reads that file many times over
    Then the keys in force never change
    And nothing is said, because an operator who is told about a rotation that did not happen stops reading the ones that did

  @edge @us-05 @slice-05
  Scenario: An instance that was never given a file never looks for one
    Given an instance with no mounted file named, which is every standalone install
    When it runs
    Then nothing re-reads anything, and no message about mounted keys is ever written
