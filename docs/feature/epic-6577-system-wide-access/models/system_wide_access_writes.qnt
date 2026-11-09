// Two System Admins write the "everyone signed in" switch and one system-wide rule at the same time.
// Safety only: a stale switch write never overwrites a newer one, and removing a rule leaves no twin behind.
module system_wide_access_writes {
  pure val CLIENTS = Set("a", "b")
  pure val NO_ROW = 0
  // Bounds the instance for exhaustive checking: at most this many accepted switch writes
  pure val MAX_WRITES = 4

  // EveryoneSignedInSetting row
  var rowExists: bool
  var everyone: bool
  var token: int
  var nextToken: int
  var seen: str -> int
  var accepted: Set[(int, int)]

  // RbacGroupMapping rows for one (group, role, AllUnits) key; the unique index does not hold for a null ScopeId
  var ruleRows: int
  var sawAbsent: str -> bool
  var lastWasRemove: bool

  action init = all {
    rowExists' = false,
    everyone' = false,
    token' = NO_ROW,
    nextToken' = 1,
    seen' = CLIENTS.mapBy(c => NO_ROW),
    accepted' = Set(),
    ruleRows' = 0,
    sawAbsent' = CLIENTS.mapBy(c => false),
    lastWasRemove' = false,
  }

  action keepRule = all {
    ruleRows' = ruleRows,
    sawAbsent' = sawAbsent,
    lastWasRemove' = false,
  }

  action keepSwitch = all {
    rowExists' = rowExists,
    everyone' = everyone,
    token' = token,
    nextToken' = nextToken,
    seen' = seen,
    accepted' = accepted,
  }

  // GET everyone-signed-in: a missing row reads as off with the empty token
  action readSwitch(c: str): bool = all {
    seen' = seen.set(c, if (rowExists) token else NO_ROW),
    rowExists' = rowExists,
    everyone' = everyone,
    token' = token,
    nextToken' = nextToken,
    accepted' = accepted,
    keepRule,
  }

  // PUT everyone-signed-in: update under the token, or insert when no row exists; anything else is a 409 (no state change)
  action putSwitch(c: str, v: bool): bool = {
    val fresh = if (rowExists) seen.get(c) == token else seen.get(c) == NO_ROW
    all {
      fresh,
      nextToken <= MAX_WRITES,
      rowExists' = true,
      everyone' = v,
      token' = nextToken,
      nextToken' = nextToken + 1,
      seen' = seen,
      accepted' = accepted.union(Set((seen.get(c), nextToken))),
      keepRule,
    }
  }

  // Create a rule: check for an equal row, then insert (two requests can both pass the check)
  action checkRule(c: str): bool = all {
    sawAbsent' = sawAbsent.set(c, ruleRows == 0),
    ruleRows' = ruleRows,
    lastWasRemove' = false,
    keepSwitch,
  }

  action insertRule(c: str): bool = all {
    sawAbsent.get(c),
    ruleRows' = ruleRows + 1,
    sawAbsent' = sawAbsent.set(c, false),
    lastWasRemove' = false,
    keepSwitch,
  }

  // Remove a rule: delete every row with the same group, role and scope
  action removeRule = all {
    ruleRows' = 0,
    sawAbsent' = sawAbsent,
    lastWasRemove' = true,
    keepSwitch,
  }

  action step = {
    nondet c = CLIENTS.oneOf()
    nondet v = Set(true, false).oneOf()
    any {
      readSwitch(c),
      putSwitch(c, v),
      checkRule(c),
      insertRule(c),
      removeRule,
    }
  }

  // No two accepted switch writes were based on the same token, so none overwrote a write it never saw
  val noLostSwitchWrite = accepted.forall(x => accepted.forall(y => x._1 == y._1 implies x == y))

  // Right after a removal no row of that rule remains, twins included
  val removalLeavesNoTwin = lastWasRemove implies ruleRows == 0

  // Twins come only from concurrent creates, so there are never more rows than creating requests
  val twinsBounded = ruleRows <= CLIENTS.size()
}
