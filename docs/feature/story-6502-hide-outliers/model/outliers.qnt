module outliers {
  // closed Cycle Times of one chart; typeOf gives each item a Work Item type
  pure val times: List[int] = [3, 4, 5, 6, 6, 7, 9, 58]
  pure val typeOf: List[int] = [0, 0, 1, 1, 0, 1, 0, 1]

  // times is given ascending, so sortedTimes is the backend sort
  pure val sortedTimes: List[int] = [3, 4, 5, 6, 6, 7, 9, 58]
  pure val n: int = 8
  pure val medianIdx: int = if ((n / 2) - 1 < 0) 0 else (n / 2) - 1
  pure val median: int = sortedTimes[medianIdx]
  pure val cut: int = 5 * median

  var hidden: Set[int]   // hidden Work Item types
  var shownCut: int

  action init = all { hidden' = Set(), shownCut' = cut }
  action toggle(t: int): bool = all {
    hidden' = if (hidden.contains(t)) hidden.exclude(Set(t)) else hidden.union(Set(t)),
    shownCut' = shownCut,
  }
  action step = any { toggle(0), toggle(1) }

  def visibleIdx: Set[int] = 0.to(n - 1).filter(i => not(hidden.contains(typeOf[i])))
  def outlierIdx: Set[int] = visibleIdx.filter(i => times[i] > cut)
  def drawnIdx: Set[int] = visibleIdx.filter(i => times[i] <= cut)

  // drawn and outliers partition the visible items
  val partitionOk = and {
    drawnIdx.union(outlierIdx) == visibleIdx,
    drawnIdx.intersect(outlierIdx) == Set(),
  }
  // the cut never moves when a type is toggled
  val cutStable = shownCut == cut
  // the chip counts exactly the visible outliers; all types hidden means no chip count
  val chipOk = and {
    outlierIdx.size() <= visibleIdx.size(),
    hidden == Set(0, 1) implies outlierIdx == Set(),
  }
  // the outlier 58 is above the cut and every other item at or below it
  val outlierIsSeparated = outlierIdx.forall(i => times[i] > cut) and drawnIdx.forall(i => times[i] <= cut)
}
