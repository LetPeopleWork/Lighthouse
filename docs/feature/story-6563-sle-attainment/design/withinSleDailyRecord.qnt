// The daily record of the share within the SLE, for one owner and one horizon.
// Atoms map to WithinSleSnapshotWriter.RecordToday / FillDayIfAbsent, the owner's SLE settings,
// the clock's day, and WithinSleShare's verdict on a stored row.
module withinSleDailyRecord {
  type Sle = { pct: int, days: int }
  type MaybeSle = NoSle | SomeSle(Sle)
  type Origin = Recorded | FilledIn
  type Row = { closed: int, within: int, sle: Sle, origin: Origin }

  pure val LAST_DAY = 3
  pure val DAYS = 1.to(LAST_DAY)
  pure val SLES = Set({ pct: 70, days: 10 }, { pct: 85, days: 14 })
  pure val MAYBE_SLES = SLES.map(s => SomeSle(s)).union(Set(NoSle))
  pure val MIN_CLOSED = 5
  pure val MOST_CLOSED = 6

  // WithinSleShare: a stored row draws a point only from MIN_CLOSED closed Work Items on.
  pure def drawsPoint(r: Row): bool = r.closed >= MIN_CLOSED

  var today: int
  var currentSle: MaybeSle
  var stored: int -> Row
  // Ghost: each past day's row as it stood when the day ended.
  var closedDays: int -> Row

  action init = all {
    today' = 1,
    nondet s = MAYBE_SLES.oneOf()
    currentSle' = s,
    stored' = Map(),
    closedDays' = Map(),
  }

  action recordToday = {
    nondet c = 0.to(MOST_CLOSED).oneOf()
    nondet w = 0.to(MOST_CLOSED).oneOf()
    all {
      w <= c,
      match currentSle {
        | SomeSle(s) => stored' = stored.put(today, { closed: c, within: w, sle: s, origin: Recorded })
        | NoSle => stored' = stored
      },
      today' = today,
      currentSle' = currentSle,
      closedDays' = closedDays,
    }
  }

  action fillDayIfAbsent = {
    nondet d = DAYS.oneOf()
    nondet c = 0.to(MOST_CLOSED).oneOf()
    nondet w = 0.to(MOST_CLOSED).oneOf()
    all {
      d <= today,
      w <= c,
      not(stored.keys().contains(d)),
      match currentSle {
        | SomeSle(s) => stored' = stored.put(d, { closed: c, within: w, sle: s, origin: FilledIn })
        | NoSle => stored' = stored
      },
      today' = today,
      currentSle' = currentSle,
      closedDays' = closedDays,
    }
  }

  action changeSle = {
    nondet s = MAYBE_SLES.oneOf()
    all {
      currentSle' = s,
      stored' = stored,
      today' = today,
      closedDays' = closedDays,
    }
  }

  action advanceDay = all {
    today < LAST_DAY,
    today' = today + 1,
    closedDays' = if (stored.keys().contains(today)) closedDays.put(today, stored.get(today)) else closedDays,
    stored' = stored,
    currentSle' = currentSle,
  }

  action step = any { recordToday, fillDayIfAbsent, changeSle, advanceDay }

  // A day that ended with a row keeps that row: its share, count, SLE and origin.
  val pastDaysNeverChange =
    closedDays.keys().forall(d => stored.keys().contains(d) and stored.get(d) == closedDays.get(d))

  // A past day that ended without a row can only have been worked out afterwards.
  val laterRowsOfPastDaysAreFilledIn =
    stored.keys().forall(d => (d < today and not(closedDays.keys().contains(d))) implies stored.get(d).origin == FilledIn)

  val withinNeverExceedsClosed = stored.keys().forall(d => stored.get(d).within <= stored.get(d).closed)

  // No point is drawn for a day with fewer than five closed Work Items.
  val noPointBelowFive = stored.keys().forall(d => drawsPoint(stored.get(d)) implies stored.get(d).closed >= 5)

  val allInvariants = pastDaysNeverChange and laterRowsOfPastDaysAreFilledIn and withinNeverExceedsClosed and noPointBelowFive
}
