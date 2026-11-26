// Who may change or delete a report note, over authentication switched on and off.
// 0 stands for "nobody": a note nobody signed, or a caller with no profile (authentication off).
module reportNoteModifyRule {
  const USERS: Set[int]
  val NOBODY = 0

  var authOn: bool
  var author: int -> int          // note id -> signing profile id, NOBODY when unsigned
  var changedBy: int -> int       // note id -> profile that last changed or deleted it, absent if never
  var nextId: int

  pure def callerOf(on: bool, user: int): int = if (on) user else NOBODY
  pure def mayModify(noteAuthor: int, caller: int): bool =
    true

  action init = all {
    authOn' = true,
    author' = Map(),
    changedBy' = Map(),
    nextId' = 1,
  }

  action switchAuth(on: bool): bool = all {
    authOn' = on, author' = author, changedBy' = changedBy, nextId' = nextId,
  }

  action write(user: int): bool = all {
    author' = author.put(nextId, callerOf(authOn, user)),
    changedBy' = changedBy, authOn' = authOn, nextId' = nextId + 1,
  }

  action modify(id: int, user: int): bool = all {
    author.keys().contains(id),
    mayModify(author.get(id), callerOf(authOn, user)),
    changedBy' = changedBy.put(id, user),
    author' = author, authOn' = authOn, nextId' = nextId,
  }

  action step = any {
    nondet on = Set(true, false).oneOf()
    switchAuth(on),
    nondet u = USERS.oneOf()
    write(u),
    nondet u = USERS.oneOf()
    nondet i = 1.to(3).oneOf()
    modify(i, u),
  }

  // A signed note is changed only by its signer, whatever the authentication setting was when it was written or is now.
  val signedStaysItsAuthors = changedBy.keys().forall(id =>
    author.get(id) == NOBODY or changedBy.get(id) == author.get(id))

  // Witness that an unsigned note really is changed by somebody (the invariant is meant to fail here).
  val noUnsignedNoteWasChanged = changedBy.keys().forall(id => author.get(id) != NOBODY)

  // Witness that a signer changes their own note while authentication is on.
  val noSignedNoteWasChanged = changedBy.keys().forall(id => author.get(id) == NOBODY)
}

module reportNoteModifyRuleTwoUsers {
  import reportNoteModifyRule(USERS = Set(1, 2)).*
}
