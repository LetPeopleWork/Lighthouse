Feature: A key store only its owner can read (Epic 5775, Slice 07 — US-07)
  As the operator running Lighthouse on a machine that has other accounts on it
  I want the files Lighthouse writes into its key store to be as closed as the ones the platform writes
  So that the protection does not depend on which of the two wrote a given file

  # Three files sit in one key store directory and all three are key material. The platform writes one
  # of them — the key that wraps the other two — and closes it to its owner. Lighthouse writes the
  # other two and leaves them at whatever the process default happens to be, which on an ordinary Linux
  # host is readable by everybody on the machine.
  #
  # This is a missing layer rather than a break: the two open files are wrapped, and the key that
  # unwraps them is the closed one. But an operator reading the directory has no way to tell that the
  # difference was accidental, and the whole point of keeping the key beside the database is that the
  # directory is the boundary. Two of the three files are not on the boundary they appear to be on.
  #
  # Carrying a key store across from an earlier version already preserves what each file had, so this
  # is confined to the two places a file is first created.

  @edge @us-07 @slice-07
  Scenario: A key this instance made for itself is readable only by the account that made it
    Given an instance with nothing in its key store yet
    When it makes a key of its own and starts
    Then the file holding that key can be read only by the account Lighthouse runs as

  @edge @us-07 @slice-07
  Scenario: The secret that signs the sign-in handshake is kept the same way
    Given an instance with nothing in its key store yet
    When it makes that secret and starts
    Then the file holding it can be read only by the account Lighthouse runs as

  @property @us-07 @slice-07
  Scenario: Every file in the key store is as closed as the most closed of them
    Given a key store an instance has filled on its own
    When the files in it are compared
    Then no file in it is open to more accounts than the key that wraps the others

  @edge @us-07 @slice-07
  Scenario: A key store carried across from an earlier version keeps what it had
    Given a key store written by a version before this one
    When Lighthouse carries it across to where it now keeps its keys
    Then every file arrives readable by exactly the accounts it was readable by before
    And nothing is opened up by the move

  @error @edge @us-07 @slice-07
  Scenario: A key store the account cannot open stops the start rather than being written around
    Given a key store whose contents this account is not allowed to read
    When Lighthouse starts
    Then it refuses to start and says which file it could not read
    And it does not write a replacement key
