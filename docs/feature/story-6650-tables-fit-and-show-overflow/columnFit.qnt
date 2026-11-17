// Column fit and edge cue of the shared grid (Story 6650).
// Abstraction: two text columns (shrink, truncate), one content column (number/date/state,
// never below its content width) and the pinned actions column (fixed, never resized).
// A text column without a stored width takes an equal share of what is left, clamped up to
// its minimum; the leftover is shared again among the unclamped ones (the grid's flex pass).
// The cue is a pure function of the horizontal scroll position of the grid's scroller.
module columnFit {
  // MUTANT = 0 is the design; other values are the behavioural mutants used for falsification.
  pure val MUTANT = 0

  var avail: int          // width the grid has for its columns
  var t1Min: int          // minimum width of text column 1
  var t2Min: int
  var contentW: int       // width that shows the content column's whole content
  var actionsW: int       // width of the pinned actions column
  var t1Stored: int       // 0 = no stored width
  var cStored: int        // 0 = no stored width for the content column
  var scrollLeft: int

  pure def max(a: int, b: int): int = if (a > b) a else b

  pure def contentWidth(content: int, stored: int): int =
    if (MUTANT == 1) (if (stored > 0) stored else content)
    else if (stored > 0) max(stored, content) else content

  // widths of the text columns: (t1, t2)
  pure def textWidths(av: int, m1: int, m2: int, s1: int, fixedW: int): (int, int) =
    val free = av - fixedW
    if (s1 > 0 and MUTANT != 2) (s1, max(m2, free - s1))
    else
      val share = free / 2
      if (share >= m1 and share >= m2) (share, free - share)
      else if (share < m1) (m1, max(m2, free - m1))
      else (max(m1, free - m2), m2)

  def cW = contentWidth(contentW, cStored)
  def tw = textWidths(avail, t1Min, t2Min, t1Stored, cW + actionsW)
  def t1W = tw._1
  def t2W = tw._2
  def total = t1W + t2W + cW + actionsW
  def scrollMax = max(0, total - avail)

  pure def cueRight(s: int, smax: int): bool = if (MUTANT == 3) s <= smax and smax >= 0 else s < smax
  pure def cueLeft(s: int): bool = s > 0
  def right = cueRight(scrollLeft, scrollMax)
  def left = cueLeft(scrollLeft)

  // Invariants
  val ContentNeverCut = cW >= contentW
  val TextNeverBelowMin = t1W >= t1Min and t2W >= t2Min
  val StoredTextWidthKept = t1Stored > 0 implies t1W == t1Stored
  val FitsWhenMinimaFit =
    (t1Stored == 0 and cStored == 0 and t1Min + t2Min + contentW + actionsW <= avail) implies total <= avail
  val NoCueWhenFits = scrollMax == 0 implies (not(right) and not(left))
  val CueAtEndGone = (scrollLeft == scrollMax) implies not(right)
  val CueOnlyWhenHidden = (right implies scrollLeft < scrollMax) and (left implies scrollLeft > 0)
  val Inv = ContentNeverCut and TextNeverBelowMin and StoredTextWidthKept and FitsWhenMinimaFit
    and NoCueWhenFits and CueAtEndGone and CueOnlyWhenHidden

  action pick = all {
    nondet a = 200.to(1200).oneOf()
    nondet m1 = 40.to(200).oneOf()
    nondet m2 = 40.to(200).oneOf()
    nondet c = 40.to(160).oneOf()
    nondet ac = 60.to(160).oneOf()
    nondet s1 = Set(0, 0, 80, 150, 300, 600).oneOf()
    nondet cs = Set(0, 20, 100, 200).oneOf()
    // stored text width respects the minimum: the resize handle cannot go below a column's minWidth
    val s1ok = if (s1 > 0) max(s1, m1) else 0
    val cw = contentWidth(c, cs)
    val tws = textWidths(a, m1, m2, s1ok, cw + ac)
    val tot = tws._1 + tws._2 + cw + ac
    nondet sl = 0.to(max(0, tot - a)).oneOf()
    all {
      avail' = a, t1Min' = m1, t2Min' = m2, contentW' = c, actionsW' = ac,
      t1Stored' = s1ok, cStored' = cs, scrollLeft' = sl,
    }
  }

  action init = pick
  action step = pick
}
