// The orphaned-Feature clean-up after maintainer decision A.
// A Feature is kept iff it is in a Portfolio or is the parent of a Feature in a Portfolio.
// A Feature in no Portfolio keeps no remaining work.
module mutant_keeps_parent_work {
  type F = str
  type P = str

  pure val FEATURES: Set[F] = Set("f1", "f2", "f3")
  pure val PORTFOLIOS: Set[P] = Set("p1", "p2")
  // The tracker's parent links; "none" when an item has no parent.
  pure val PARENT: F -> F = Map("f1" -> "f2", "f2" -> "f3", "f3" -> "none")

  var stored: Set[F]
  var member: F -> Set[P]
  var flagged: Set[F]
  var work: Set[F]
  var portfolios: Set[P]
  var clean: bool
  var neededDeleted: bool

  def inPortfolio(f: F): bool = member.get(f) != Set()
  def needed(f: F): bool = stored.exists(c => inPortfolio(c) and PARENT.get(c) == f)

  // The rule under design.
  def keep(f: F): bool = inPortfolio(f) or needed(f)

  action init = all {
    stored' = Set(),
    member' = FEATURES.mapBy(_ => Set()),
    flagged' = Set(),
    work' = Set(),
    portfolios' = PORTFOLIOS,
    clean' = true,
    neededDeleted' = false,
  }

  action joinPortfolio(f: F, p: P): bool = all {
    portfolios.contains(p),
    stored' = stored.union(Set(f)),
    member' = member.set(f, member.get(f).union(Set(p))),
    flagged' = flagged,
    work' = work.union(Set(f)),
    portfolios' = portfolios,
    clean' = false,
    neededDeleted' = neededDeleted,
  }

  // Before the fix nothing clears remaining work here; the clean-up owns it.
  action leavePortfolio(f: F, p: P): bool = all {
    member.get(f).contains(p),
    stored' = stored,
    member' = member.set(f, member.get(f).exclude(Set(p))),
    flagged' = flagged,
    work' = work,
    portfolios' = portfolios,
    clean' = false,
    neededDeleted' = neededDeleted,
  }

  // The parent half stores a parent it has not got yet, flagged.
  action parentHalf(c: F): bool = all {
    stored.contains(c),
    inPortfolio(c),
    PARENT.get(c) != "none",
    not(stored.contains(PARENT.get(c))),
    stored' = stored.union(Set(PARENT.get(c))),
    member' = member,
    flagged' = flagged.union(Set(PARENT.get(c))),
    work' = work,
    portfolios' = portfolios,
    clean' = false,
    neededDeleted' = neededDeleted,
  }

  def inP(m: F -> Set[P], f: F): bool = m.get(f) != Set()
  def need(m: F -> Set[P], f: F): bool = stored.exists(c => inP(m, c) and PARENT.get(c) == f)
  def keptAfter(m: F -> Set[P]): Set[F] = stored.filter(f => inP(m, f) or need(m, f))

  action cleanupAfter(m: F -> Set[P], ps: Set[P]): bool = all {
    stored' = keptAfter(m),
    member' = m,
    flagged' = flagged.intersect(keptAfter(m)),
    work' = work.filter(f => keptAfter(m).contains(f)),
    portfolios' = ps,
    clean' = true,
    neededDeleted' = (neededDeleted or stored.exists(f => need(m, f) and not(keptAfter(m).contains(f)))),
  }

  action cleanup: bool = cleanupAfter(member, portfolios)

  // Deleting a Portfolio drops its memberships and runs the clean-up at once.
  action deletePortfolio(p: P): bool = all {
    portfolios.contains(p),
    cleanupAfter(FEATURES.mapBy(f => member.get(f).exclude(Set(p))), portfolios.exclude(Set(p))),
  }

  action step = any {
    nondet f = FEATURES.oneOf()
    nondet p = PORTFOLIOS.oneOf()
    any { joinPortfolio(f, p), leavePortfolio(f, p), parentHalf(f), deletePortfolio(p) },
    cleanup,
  }

  // After a clean-up every stored Feature is in a Portfolio or parents a Portfolio Feature.
  val cleanKeepsOnlyMembersAndParents: bool =
    clean implies stored.forall(f => inPortfolio(f) or needed(f))

  // After a clean-up no Feature outside every Portfolio carries remaining work.
  val noWorkOutsidePortfolios: bool =
    clean implies work.forall(f => inPortfolio(f))

  // No clean-up ever deletes the parent of a Feature in a Portfolio (no churn).
  val neverDeletesANeededParent: bool = not(neededDeleted)

  // Work only on stored Features.
  val workOnStoredOnly: bool = work.subseteq(stored)

  val allInvariants: bool = cleanKeepsOnlyMembersAndParents and noWorkOutsidePortfolios and neverDeletesANeededParent and workOnStoredOnly
}
