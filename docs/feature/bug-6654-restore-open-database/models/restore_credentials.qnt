// Restore of a backup under the credential pre-check (Bug 6654, slices S1 and S2).
// A database is the set of its stored secrets, each a (value id, readable with this installation's key) pair,
// plus whether its schema is too old for the check to be asked (the probe then answers CannotTell).
module restore_credentials {
  type Db = { secrets: Set[(int, bool)], legacy: bool }

  var live: Db
  var start: Db
  var backup: Db
  var optIn: bool
  var finding: str
  var phase: str

  pure val Values = Set((1, true), (1, false), (2, true), (2, false))

  pure def readability(d: Db): str =
    if (d.legacy) "CannotTell"
    else if (d.secrets == Set()) "NothingStored"
    else if (d.secrets.exists(s => s._2)) "SomethingReadable"
    else "NothingReadable"

  // The startup refusal of EncryptionKeyRingBootstrapper fires on NothingReadable only.
  pure def startable(d: Db): bool = readability(d) != "NothingReadable"

  pure def removesUnreadable(o: bool, f: str): bool = o and f == "NothingReadable"

  action init = {
    nondet ls = Values.powerset().oneOf()
    nondet ll = Set(true, false).oneOf()
    nondet bs = Values.powerset().oneOf()
    nondet bl = Set(true, false).oneOf()
    nondet o = Set(true, false).oneOf()
    val l = { secrets: ls, legacy: ll }
    all {
      startable(l),
      live' = l,
      start' = l,
      backup' = { secrets: bs, legacy: bl },
      optIn' = o,
      finding' = "",
      phase' = "Idle",
    }
  }

  // DatabaseManagementService.RestoreBackup: probe the staged backup before the provider touches the live database.
  action check = {
    val f = readability(backup)
    all {
      phase == "Idle",
      finding' = f,
      phase' = if (f == "NothingReadable" and not(optIn)) "Stopped" else "Checked",
      live' = live, start' = start, backup' = backup, optIn' = optIn,
    }
  }

  // IDatabaseManagementProvider.RestoreBackup: SQLite online backup API / pg_restore.
  action copy = all {
    phase == "Checked",
    live' = backup,
    phase' = "Copied",
    start' = start, backup' = backup, optIn' = optIn, finding' = finding,
  }

  // UnreadableStoredSecrets.RemoveFrom on the live connection, only after an opted-in NothingReadable finding.
  action scrub = all {
    phase == "Copied",
    live' = if (removesUnreadable(optIn, finding)) { ...live, secrets: live.secrets.filter(s => s._2) } else live,
    phase' = "Done",
    start' = start, backup' = backup, optIn' = optIn, finding' = finding,
  }

  action crash = all {
    phase.in(Set("Checked", "Copied")),
    phase' = "Crashed",
    live' = live, start' = start, backup' = backup, optIn' = optIn, finding' = finding,
  }

  action settle = all {
    phase.in(Set("Stopped", "Done", "Crashed")),
    live' = live, start' = start, backup' = backup, optIn' = optIn, finding' = finding, phase' = phase,
  }

  action step = any { check, copy, scrub, crash, settle }

  val StopChangesNothing = phase == "Stopped" implies live == start

  val DoneIsStartable = phase == "Done" implies startable(live)

  val DoneWithoutRemovalEqualsBackup =
    (phase == "Done" and not(removesUnreadable(optIn, finding))) implies live == backup

  val RemovalTakesOnlyUnreadable =
    phase == "Done" implies all {
      live.secrets.subseteq(backup.secrets),
      backup.secrets.filter(s => s._2).subseteq(live.secrets),
      live.legacy == backup.legacy,
    }

  val StopOnlyOnNothingReadable = phase == "Stopped" implies (finding == "NothingReadable" and not(optIn))

  val Safety = all {
    StopChangesNothing,
    DoneIsStartable,
    DoneWithoutRemovalEqualsBackup,
    RemovalTakesOnlyUnreadable,
    StopOnlyOnNothingReadable,
  }
}
