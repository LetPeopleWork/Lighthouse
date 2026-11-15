// S1 grid: deterministic evaluation of R1-R3 over the PO bounds, today = day 1000.
module s1grid {
  import baseline.* from "./baseline"
  pure val T = 1000
  // rows: L, days since Then ended (L = back-to-back default), cutoff relative to Then's first day
  pure val grid = tuples(Set(14, 30, 90), Set(-1, 0, 15, 45, 14, 30, 90), Set(-1, 0, 1)).filter(((l, s, c)) =>
      s == -1 or s == 0 or s == 15 or s == 45 or s == l).map(((l, s, c)) => {
    val te = T - s
    val ts = te - l + 1
    val cut = ts + c
    val res = validate(te, l, ABSENT, T, cut)
    val ns = T - l + 1
    {
      L: l, sinceThenEnded: s, isDefault: s == l, cutoffMinusThenStart: c,
      thenStart: ts, thenEnd: te, nowStart: ns, nowEnd: T, result: res,
      overlaps: te >= ns, overlapUntil: if (te >= ns) te else -1, sharedDays: if (te >= ns) te - ns + 1 else 0
    }
  })
  // S3: Then total t, Now total t+1, same per-day value at 1 decimal (t in 0..60)
  pure val s3SameCount = Set(14, 30, 90).map(l => (l, 0.to(60).filter(t => perDayTenths(t, l) == perDayTenths(t + 1, l)).size()))
  pure val s3SameLowest = Set(14, 30, 90).map(l => (l, 0.to(60).filter(t => perDayTenths(t, l) == perDayTenths(t + 1, l)).fold(9999, (a, t) => if (t < a) t else a)))
  pure val s3Examples = tuples(Set(14, 30, 90), Set(0, 1, 2, 7, 14, 29, 30, 44, 45, 89, 90))
    .filter(((l, t)) => perDayTenths(t, l) == perDayTenths(t + 1, l))
    .map(((l, t)) => (l, t, t + 1, perDayTenths(t, l)))
  pure val tooShort = Set(7, 13).map(l => validate(T - l, l, ABSENT, T, 0))
}
