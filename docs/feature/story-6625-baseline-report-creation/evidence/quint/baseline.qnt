// Baseline "Then & Now" report, Story 6625. Days are integers; day 1000 = 2026-11-15.
// Windows are inclusive at both ends. Built only from the product rules R1-R7.
module baseline {
  type Report = {
    thenEnd: int, len: int,
    nowLen: int,        // stored (legacy) Now length; R2 says the shown Now ignores it
    version: int,
    thenTotal: int, nowTotal: int  // -1 = not captured
  }
  type Event = { kind: str, id: int, thenEnd: int, len: int, nowLen: int, result: str }

  pure val LENS = Set(14, 30, 90)
  pure val SINCE = Set(0, 15, 45)          // days since Then ended
  pure val TOTALS = Set(-1, 0, 1, 2, 3, 7, 14, 15, 29, 30, 31, 45, 90, 91)
  pure val ABSENT = -1

  var today: int
  var cutoff: int
  var reports: int -> Report
  var nextId: int
  var migrated: bool
  var migrationBumpsVersion: bool          // rules silent: modelled both ways
  var edit: { open: bool, id: int, baseVersion: int, draftNowLen: int }
  var event: Event

  // ---------- derived (what the screen shows) ----------
  pure def thenStart(r: Report): int = r.thenEnd - r.len + 1
  def nowStart(r: Report): int = today - r.len + 1           // R2
  // UI rule for the overlap message: Then's last day is on/after Now's first day
  def overlapShown(r: Report): bool = r.thenEnd >= nowStart(r)
  // overlap end date shown = last day of Then (= last shared day, since Then ends <= today)
  def overlapUntil(r: Report): int = r.thenEnd
  // independent formulation: the two inclusive intervals intersect
  pure def max2(a: int, b: int): int = if (a > b) a else b
  pure def min2(a: int, b: int): int = if (a < b) a else b
  def shareDay(r: Report): bool =
    max2(thenStart(r), nowStart(r)) <= min2(r.thenEnd, today)
  def lastSharedDay(r: Report): int = min2(r.thenEnd, today)

  pure def validate(thenEnd: int, len: int, nowLen: int, t: int, c: int): str =
    if (len < 14) "refused: Then shorter than 14 days"
    else if (thenEnd > t) "refused: Then ends after today"
    else if (thenEnd - len + 1 < c) "refused: Then starts before kept history"
    else if (nowLen != ABSENT and nowLen != len) "refused: Now length differs from Then length"
    else "ok"

  // R6 per-day in tenths, rounded half up: round(10*t/L)
  pure def perDayTenths(t: int, len: int): int = (20 * t + len) / (2 * len)
  pure def bothCaptured(r: Report): bool = r.thenTotal >= 0 and r.nowTotal >= 0
  pure def totalLineKind(r: Report): str =
    if (r.thenTotal < 0 or r.nowTotal < 0) "no-change"
    else if (r.thenTotal == 0) "then-was-0"
    else "pct"
  pure def iabs(x: int): int = if (x < 0) -x else x

  pure def ev(k: str, i: int, te: int, l: int, n: int, res: str): Event =
    { kind: k, id: i, thenEnd: te, len: l, nowLen: n, result: res }

  // ---------- init: one saved report from before this Story ----------
  action init = {
    nondet l = LENS.oneOf()
    nondet since = SINCE.oneOf()
    nondet oldNow = Set(l, 45).oneOf()
    nondet cut = Set(0, 135, 150).oneOf()   // days before today where kept history starts
    nondet bumps = Set(true, false).oneOf()
    all {
      today' = 1000,
      cutoff' = 1000 - cut,
      reports' = Map(1 -> { thenEnd: 1000 - since, len: l, nowLen: oldNow, version: 1,
                            thenTotal: -1, nowTotal: -1 }),
      nextId' = 2,
      migrated' = false,
      migrationBumpsVersion' = bumps,
      edit' = { open: false, id: 0, baseVersion: 0, draftNowLen: ABSENT },
      event' = ev("init", 1, 1000 - since, l, oldNow, "saved before this Story"),
    }
  }

  // R1/R3: UI create with the default (back to back) or Then ending today
  action uiCreate = {
    nondet l = Set(7, 14, 30, 90).oneOf()
    nondet choice = Set("ui-create-default", "ui-create-ends-today").oneOf()
    val te = if (choice == "ui-create-default") today - l else today
    val res = validate(te, l, ABSENT, today, cutoff)
    all {
      nextId < 4,
      reports' = if (res == "ok") reports.put(nextId, { thenEnd: te, len: l, nowLen: l, version: 1,
                                                        thenTotal: -1, nowTotal: -1 }) else reports,
      nextId' = if (res == "ok") nextId + 1 else nextId,
      event' = ev(choice, nextId, te, l, ABSENT, res),
      today' = today, cutoff' = cutoff, migrated' = migrated,
      migrationBumpsVersion' = migrationBumpsVersion, edit' = edit,
    }
  }

  // R1/R5: API create with arbitrary Then end and optional Now length
  action apiCreate = {
    nondet l = Set(7, 14, 30, 90).oneOf()
    nondet since = Set(-1, 0, 15, 45).oneOf()
    nondet nl = Set(ABSENT, l, 45).oneOf()
    val te = today - since
    val res = validate(te, l, nl, today, cutoff)
    all {
      nextId < 4,
      reports' = if (res == "ok") reports.put(nextId, { thenEnd: te, len: l, nowLen: l, version: 1,
                                                        thenTotal: -1, nowTotal: -1 }) else reports,
      nextId' = if (res == "ok") nextId + 1 else nextId,
      event' = ev("api-create", nextId, te, l, nl, res),
      today' = today, cutoff' = cutoff, migrated' = migrated,
      migrationBumpsVersion' = migrationBumpsVersion, edit' = edit,
    }
  }

  // An Edit is opened in a browser. draftNowLen: an old page still sends the loaded Now length,
  // a new page sends none.
  action openEdit = {
    nondet id = reports.keys().oneOf()
    nondet oldPage = Set(true, false).oneOf()
    val r = reports.get(id)
    val dn = if (oldPage) r.nowLen else ABSENT
    all {
      not(edit.open),
      edit' = { open: true, id: id, baseVersion: r.version, draftNowLen: dn },
      event' = ev(if (oldPage) "open-edit-old-page" else "open-edit", id, r.thenEnd, r.len, dn, "open"),
      today' = today, cutoff' = cutoff, reports' = reports, nextId' = nextId, migrated' = migrated,
      migrationBumpsVersion' = migrationBumpsVersion,
    }
  }

  // Save the open Edit, possibly with a changed Then. An old page keeps the Now length it loaded.
  action saveEdit = {
    nondet newLen = LENS.oneOf()
    nondet since = SINCE.oneOf()
    val r = reports.get(edit.id)
    val te = today - since
    val stale = r.version != edit.baseVersion
    val res = if (stale) "refused: report changed since the Edit was opened"
              else validate(te, newLen, edit.draftNowLen, today, cutoff)
    all {
      edit.open,
      reports' = if (res == "ok") reports.put(edit.id, { ...r, thenEnd: te, len: newLen, nowLen: newLen,
                                                         version: r.version + 1 }) else reports,
      edit' = { open: false, id: 0, baseVersion: 0, draftNowLen: ABSENT },
      event' = ev("save-edit", edit.id, te, newLen, edit.draftNowLen, res),
      today' = today, cutoff' = cutoff, nextId' = nextId, migrated' = migrated,
      migrationBumpsVersion' = migrationBumpsVersion,
    }
  }

  // R4: run once, set every saved report's Now length to its Then length
  action migrate = all {
    not(migrated),
    reports' = reports.keys().mapBy(k => {
      val r = reports.get(k)
      if (r.nowLen != r.len)
        { ...r, nowLen: r.len, version: if (migrationBumpsVersion) r.version + 1 else r.version }
      else r
    }),
    migrated' = true,
    event' = ev("migrate", 0, 0, 0, 0, if (migrationBumpsVersion) "version bumped" else "version kept"),
    today' = today, cutoff' = cutoff, nextId' = nextId, edit' = edit,
    migrationBumpsVersion' = migrationBumpsVersion,
  }

  // R7: days advance; Then frozen, Now slides
  action advance = {
    nondet d = Set(1, 15, 45).oneOf()
    all {
      today' = today + d,
      event' = ev("advance", 0, 0, d, 0, "days advanced"),
      cutoff' = cutoff, reports' = reports, nextId' = nextId, migrated' = migrated,
      migrationBumpsVersion' = migrationBumpsVersion, edit' = edit,
    }
  }

  // kept history moves forward (retention); rules silent about saved reports then
  action retention = {
    nondet d = Set(15, 45).oneOf()
    all {
      cutoff' = cutoff + d,
      event' = ev("retention", 0, 0, d, 0, "kept history start moved"),
      today' = today, reports' = reports, nextId' = nextId, migrated' = migrated,
      migrationBumpsVersion' = migrationBumpsVersion, edit' = edit,
    }
  }

  // R6: throughput data arrives for a report's windows
  action totals = {
    nondet id = reports.keys().oneOf()
    nondet tt = TOTALS.oneOf()
    nondet nt = TOTALS.oneOf()
    all {
      reports' = reports.put(id, { ...reports.get(id), thenTotal: tt, nowTotal: nt }),
      event' = ev("totals", id, tt, nt, 0, "totals recorded"),
      today' = today, cutoff' = cutoff, nextId' = nextId, migrated' = migrated,
      migrationBumpsVersion' = migrationBumpsVersion, edit' = edit,
    }
  }

  action step = any { uiCreate, apiCreate, openEdit, saveEdit, migrate, advance, retention, totals }

  // ---------- invariants ----------
  val invNowEqThenAfterMigration = migrated implies reports.keys().forall(k => reports.get(k).nowLen == reports.get(k).len)
  val invOverlapIffShareDay = reports.keys().forall(k => overlapShown(reports.get(k)) == shareDay(reports.get(k)))
  val invOverlapUntilIsLastSharedDay = reports.keys().forall(k =>
    overlapShown(reports.get(k)) implies overlapUntil(reports.get(k)) == lastSharedDay(reports.get(k)))
  val invChangeIffBothCaptured = reports.keys().forall(k =>
    (totalLineKind(reports.get(k)) != "no-change") == bothCaptured(reports.get(k)))
  val invPctOnlyWhenThenPositive = reports.keys().forall(k =>
    totalLineKind(reports.get(k)) == "pct" implies reports.get(k).thenTotal > 0)
  val invApiAcceptOnlyEqualNow = (event.kind == "api-create" and event.result == "ok") implies
    (event.nowLen == ABSENT or event.nowLen == event.len)
  val invSavedThenAtLeast14 = reports.keys().forall(k => reports.get(k).len >= 14)
  val invThenNotAfterToday = reports.keys().forall(k => reports.get(k).thenEnd <= today)
  // expected to break (probes, not rules):
  val invThenInsideKeptHistory = reports.keys().forall(k => thenStart(reports.get(k)) >= cutoff)
  val invShownNowEqStoredNow = reports.keys().forall(k => reports.get(k).nowLen == reports.get(k).len)
  val invEditSaveNotRefused = not(event.kind == "save-edit" and event.result != "ok")

  // ---------- witnesses (negate to get a concrete trace) ----------
  val wPerDaySameTotalBy1 = reports.keys().exists(k => {
    val r = reports.get(k)
    bothCaptured(r) and iabs(r.nowTotal - r.thenTotal) == 1 and
      perDayTenths(r.nowTotal, r.len) == perDayTenths(r.thenTotal, r.len)
  })
  val wThenWas0 = reports.keys().exists(k => totalLineKind(reports.get(k)) == "then-was-0" and reports.get(k).nowTotal > 0)
  val wEqualTotals = reports.keys().exists(k => bothCaptured(reports.get(k)) and reports.get(k).thenTotal == reports.get(k).nowTotal and reports.get(k).thenTotal > 0)
  val wNotCaptured = reports.keys().exists(k => not(bothCaptured(reports.get(k))) and (reports.get(k).thenTotal >= 0 or reports.get(k).nowTotal >= 0))
  val wOverlapThenEnded = reports.keys().exists(k => reports.get(k).thenEnd == 1000 and not(overlapShown(reports.get(k))))
  val wDefaultRefused = event.kind == "ui-create-default" and event.result != "ok"
  val wEndsTodayOk = event.kind == "ui-create-ends-today" and event.result == "ok"
  val wApiNowLenRefused = event.kind == "api-create" and event.result == "refused: Now length differs from Then length"
  val wApiAfterToday = event.kind == "api-create" and event.result == "refused: Then ends after today"
  val wOldEditRefusedAfterMigration = migrated and event.kind == "save-edit" and event.result != "ok"
}
