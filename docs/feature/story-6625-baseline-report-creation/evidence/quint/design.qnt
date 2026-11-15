// Story 6625 DESIGN model. Days are integers; day 1000 = 2026-11-15; windows inclusive at both ends.
// Design under test: the server derives Now's length from Then's length on every read; no stored Now length
// is read, none is written on create, no snap migration runs, so no report's concurrency version changes at deploy.
// MUTANT selects one behavioural change for falsification runs (copies with another value are made by runmutants.sh).
module design {
  pure val MUTANT = "none"
  pure val ABSENT = -1
  pure val LENS = Set(14, 30, 45, 90)
  pure val SINCE = Set(0, 15, 31, 45)
  pure val TOTALS = Set(0, 1, 13, 29, 30, 52, 65, 91)

  type Report = { thenEnd: int, len: int, storedNowLen: int, version: int }
  type Edit = { open: bool, id: int, baseVersion: int, oldPage: bool, draftNowLen: int, sawConcurrent: bool }
  type Event = { kind: str, oldPage: bool, len: int, nowLen: int, result: str, sawConcurrent: bool }

  var today: int
  var cutoff: int
  var deployed: bool
  var reports: int -> Report
  var nextId: int
  var edit: Edit
  var event: Event
  var tThen: int
  var tNow: int
  var tLen: int

  pure def noEdit: Edit = { open: false, id: 0, baseVersion: 0, oldPage: false, draftNowLen: ABSENT, sawConcurrent: false }
  pure def ev(k: str, op: bool, l: int, n: int, res: str, sc: bool): Event =
    { kind: k, oldPage: op, len: l, nowLen: n, result: res, sawConcurrent: sc }

  // Now length a reader is shown. Before deploy the old version reads the stored member; after, it is derived.
  def shownNowLen(r: Report): int =
    if (deployed and MUTANT != "read-stored") r.len
    else if (r.storedNowLen >= 0) r.storedNowLen
    else r.len

  // Create refusal order as ThenAndNowTemplate.Freeze: Then length, Then end, kept history, then the Now rule.
  pure def createRefusal(thenEnd: int, len: int, nowLen: int, t: int, c: int): str =
    if (len < 14) "then-too-short"
    else if (thenEnd > t) "then-ends-in-the-future"
    else if (thenEnd - len + 1 < c and MUTANT != "skip-history") "then-before-cutoff"
    else if (nowLen != ABSENT and nowLen != len and MUTANT != "skip-now-check") "now-length-differs-from-then"
    else "ok"

  // Edit refusal after deploy: stale token, then the Now rule. Then is never checked against kept history.
  def editRefusal(r: Report, e: Edit, c: int): str =
    if (r.version != e.baseVersion) "stale"
    else if (MUTANT == "edit-checks-history" and r.thenEnd - r.len + 1 < c) "then-before-cutoff"
    else if (e.draftNowLen != ABSENT and e.draftNowLen != r.len and MUTANT != "skip-now-check") "now-length-differs-from-then"
    else "ok"

  // Per day in hundredths, rounded half away from zero (totals are non-negative here).
  pure def perDayHundredths(total: int, len: int): int = (200 * total + len) / (2 * len)
  pure def sign(x: int): int = if (x > 0) 1 else if (x < 0) -1 else 0
  def nowLenForPerDay: int = if (MUTANT == "unequal-lengths") tLen + 15 else tLen

  action keepTotals = all { tThen' = tThen, tNow' = tNow, tLen' = tLen }

  action init = {
    nondet since = SINCE.oneOf()
    nondet len = LENS.oneOf()
    nondet stored = Set(ABSENT, 30, 45, 90).oneOf()
    nondet c = Set(800, 960).oneOf()
    all {
      today' = 1000, cutoff' = c, deployed' = false,
      reports' = Map(1 -> { thenEnd: 1000 - since, len: len, storedNowLen: stored, version: 1 }),
      nextId' = 2, edit' = noEdit, event' = ev("init", false, len, stored, "ok", false),
      tThen' = 0, tNow' = 0, tLen' = 30,
    }
  }

  // The new version starts. The design runs no snap; the BUMP mutant models a snap that bumps versions.
  action deploy = all {
    not(deployed),
    deployed' = true,
    reports' = if (MUTANT == "bump") reports.keys().mapBy(k => {
                 val r = reports.get(k)
                 if (r.storedNowLen >= 0 and r.storedNowLen != r.len) { ...r, storedNowLen: r.len, version: r.version + 1 } else r
               }) else reports,
    event' = ev("deploy", false, 0, 0, "ok", false),
    today' = today, cutoff' = cutoff, nextId' = nextId, edit' = edit, keepTotals,
  }

  action create = {
    nondet since = SINCE.oneOf()
    nondet len = LENS.oneOf()
    nondet nl = Set(ABSENT, 0).oneOf()
    nondet other = LENS.oneOf()
    val nowLen = if (nl == ABSENT) ABSENT else other
    val te = today - since
    val res = createRefusal(te, len, nowLen, today, cutoff)
    val stored = if (MUTANT == "store-now") nowLen else ABSENT
    all {
      deployed, nextId < 4,
      reports' = if (res == "ok") reports.put(nextId, { thenEnd: te, len: len, storedNowLen: stored, version: 1 }) else reports,
      nextId' = if (res == "ok") nextId + 1 else nextId,
      event' = ev("create", false, len, nowLen, res, false),
      today' = today, cutoff' = cutoff, deployed' = deployed, edit' = edit, keepTotals,
    }
  }

  // A page opened before deploy shows the old Now step and resends the Now length it showed; a new page sends none.
  action openEdit = {
    nondet id = reports.keys().oneOf()
    val r = reports.get(id)
    all {
      not(edit.open),
      edit' = { open: true, id: id, baseVersion: r.version, oldPage: not(deployed),
                draftNowLen: if (deployed) ABSENT else shownNowLen(r), sawConcurrent: false },
      event' = ev("open-edit", not(deployed), r.len, ABSENT, "open", false),
      today' = today, cutoff' = cutoff, deployed' = deployed, reports' = reports, nextId' = nextId, keepTotals,
    }
  }

  // Somebody else saves the same report while this Edit is open.
  action concurrentSave = {
    val r = reports.get(edit.id)
    all {
      edit.open, deployed,
      reports' = reports.put(edit.id, { ...r, version: r.version + 1 }),
      edit' = { ...edit, sawConcurrent: true },
      event' = ev("concurrent-save", false, r.len, ABSENT, "ok", false),
      today' = today, cutoff' = cutoff, deployed' = deployed, nextId' = nextId, keepTotals,
    }
  }

  action saveEdit = {
    val r = reports.get(edit.id)
    val res = editRefusal(r, edit, cutoff)
    all {
      edit.open, deployed,
      reports' = if (res == "ok") reports.put(edit.id, { ...r, version: r.version + 1 }) else reports,
      edit' = noEdit,
      event' = ev("save-edit", edit.oldPage, r.len, edit.draftNowLen, res, edit.sawConcurrent),
      today' = today, cutoff' = cutoff, deployed' = deployed, nextId' = nextId, keepTotals,
    }
  }

  action advance = all {
    today' = today + 1, cutoff' = cutoff + 1,
    event' = ev("advance", false, 0, 0, "ok", false),
    deployed' = deployed, reports' = reports, nextId' = nextId, edit' = edit, keepTotals,
  }

  action totals = {
    nondet a = TOTALS.oneOf()
    nondet b = TOTALS.oneOf()
    nondet l = LENS.oneOf()
    all {
      tThen' = a, tNow' = b, tLen' = l,
      event' = ev("totals", false, l, l, "ok", false),
      today' = today, cutoff' = cutoff, deployed' = deployed, reports' = reports, nextId' = nextId, edit' = edit,
    }
  }

  action step = any { deploy, create, openEdit, concurrentSave, saveEdit, advance, totals }

  // After deploy every report, legacy or new, shows a Now exactly as long as its Then.
  val invShownNowEqThen = deployed implies reports.keys().forall(k => shownNowLen(reports.get(k)) == reports.get(k).len)
  // The new version never stores a Now length.
  val invNoStoredNowOnCreate = reports.keys().forall(k => k >= 2 implies reports.get(k).storedNowLen == ABSENT)
  // An accepted create or edit carried no Now length or one equal to Then's.
  val invAcceptOnlyEqualOrAbsentNow =
    ((event.kind == "create" or event.kind == "save-edit") and event.result == "ok") implies
      (event.nowLen == ABSENT or event.nowLen == event.len)
  // An edit is refused as stale only when somebody else really saved meanwhile: the deploy alone never makes it stale.
  val invStaleOnlyAfterRealChange = (event.kind == "save-edit" and event.result == "stale") implies event.sawConcurrent
  // Edit is never refused for kept history.
  val invEditNeverRefusedForHistory = not(event.kind == "save-edit" and event.result == "then-before-cutoff")
  // An accepted create's Now (same length, ending today) starts inside kept history.
  val invCreatedNowInsideKeptHistory = (event.kind == "create" and event.result == "ok") implies today - event.len + 1 >= cutoff
  // With equal lengths, the Total line and the per-day line never move in opposite directions.
  val invTotalAndPerDayAgree =
    sign(tNow - tThen) * sign(perDayHundredths(tNow, nowLenForPerDay) - perDayHundredths(tThen, tLen)) >= 0

  // Witnesses (expected to be violated: they show the behaviour is reachable).
  val wOldPageNowRefused = not(event.kind == "save-edit" and event.oldPage and event.result == "now-length-differs-from-then")
  val wLegacyOverHistoryEdited = not(event.kind == "save-edit" and event.result == "ok" and cutoff == 960)
}
