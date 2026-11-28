// One shared read (the permissions summary, or the Teams, Portfolios or licence list) behind many components.
// Observer 1 is the app shell, which stays mounted across page moves. The others belong to a page; on a page move
// React may keep any of them mounted (same type in the same place), so each one either unmounts or stays.
module shared_page_reads {
  pure val MaxPage = 3
  pure val Shell = 1
  pure val Observers = Set(1, 2, 3)
  pure val Unmounted = -1

  type Cached = { status: str, askedOn: int, includesOwn: int }
  type Request = { active: bool, askedOn: int, includesOwn: int }

  var page: int
  var cache: Cached
  var inflight: Request
  var mountedOn: int -> int
  var ownTotal: int
  var requestsOnPage: int
  var retriesOnPage: int
  var ownChangesOnPage: int
  // Ghost: whether each component showed an answer in the previous state (no-flicker check only).
  var wasReady: int -> bool

  // The answer a component may show: asked on or after the page it mounted on. A component that stays mounted
  // across a page move keeps its earlier mount page, so it keeps showing the latest answer while the move's ask runs.
  def isReady(o: int): bool =
    mountedOn.get(o) != Unmounted and cache.status == "ready" and cache.askedOn >= mountedOn.get(o)

  val readyNow: int -> bool = Observers.mapBy(o => isReady(o))

  action init = all {
    page' = 0,
    cache' = { status: "none", askedOn: Unmounted, includesOwn: 0 },
    inflight' = { active: true, askedOn: 0, includesOwn: 0 },
    mountedOn' = Observers.mapBy(o => if (o == Shell) 0 else Unmounted),
    ownTotal' = 0,
    requestsOnPage' = 1,
    retriesOnPage' = 0,
    ownChangesOnPage' = 0,
    wasReady' = Observers.mapBy(o => false),
  }

  // The user moves to another page: page components unmount unless React keeps them (staying), the shared read
  // is asked again once, and a request still in flight from the previous page is cancelled.
  action movesToAnotherPage(staying: Set[int]): bool = all {
    page < MaxPage,
    page' = page + 1,
    inflight' = if (inflight.active) inflight else { active: true, askedOn: page + 1, includesOwn: ownTotal },
    mountedOn' = Observers.mapBy(o => if (o == Shell or staying.contains(o)) mountedOn.get(o) else Unmounted),
    wasReady' = readyNow,
    requestsOnPage' = 1,
    retriesOnPage' = 0,
    ownChangesOnPage' = 0,
    cache' = cache,
    ownTotal' = ownTotal,
  }

  // A component mounts on the current page and reads the shared cache; it asks nothing itself.
  action componentMounts(o: int): bool = all {
    wasReady' = readyNow,
    mountedOn.get(o) == Unmounted,
    mountedOn' = mountedOn.set(o, page),
    page' = page, cache' = cache, inflight' = inflight, ownTotal' = ownTotal,
    requestsOnPage' = requestsOnPage, retriesOnPage' = retriesOnPage, ownChangesOnPage' = ownChangesOnPage,
  }

  action answerArrives = all {
    wasReady' = readyNow,
    inflight.active,
    cache' = { status: "ready", askedOn: inflight.askedOn, includesOwn: inflight.includesOwn },
    inflight' = { ...inflight, active: false },
    page' = page, mountedOn' = mountedOn, ownTotal' = ownTotal,
    requestsOnPage' = requestsOnPage, retriesOnPage' = retriesOnPage, ownChangesOnPage' = ownChangesOnPage,
  }

  action requestFails = all {
    wasReady' = readyNow,
    inflight.active,
    cache' = { ...cache, status: "failed" },
    inflight' = { ...inflight, active: false },
    page' = page, mountedOn' = mountedOn, ownTotal' = ownTotal,
    requestsOnPage' = requestsOnPage, retriesOnPage' = retriesOnPage, ownChangesOnPage' = ownChangesOnPage,
  }

  // One retry asks again once, for every component.
  action userRetries = all {
    wasReady' = readyNow,
    cache.status == "failed",
    not(inflight.active),
    retriesOnPage < 2,
    inflight' = { active: true, askedOn: page, includesOwn: ownTotal },
    requestsOnPage' = requestsOnPage + 1,
    retriesOnPage' = retriesOnPage + 1,
    page' = page, cache' = cache, mountedOn' = mountedOn, ownTotal' = ownTotal,
    ownChangesOnPage' = ownChangesOnPage,
  }

  // The user's own create, rename, delete or grant completes: the shared read is asked again at once.
  action ownChangeCompletes = all {
    wasReady' = readyNow,
    ownChangesOnPage < 2,
    ownTotal' = ownTotal + 1,
    inflight' = { active: true, askedOn: page, includesOwn: ownTotal + 1 },
    requestsOnPage' = requestsOnPage + 1,
    ownChangesOnPage' = ownChangesOnPage + 1,
    page' = page, cache' = cache, mountedOn' = mountedOn, retriesOnPage' = retriesOnPage,
  }

  action step = any {
    nondet staying = oneOf(Observers.powerset())
    movesToAnotherPage(staying),
    nondet o = oneOf(Observers)
    componentMounts(o),
    answerArrives,
    requestFails,
    userRetries,
    ownChangeCompletes,
  }

  // A component that mounts on the current page never takes an answer asked on an earlier page as current.
  val newComponentsSeeOnlyThisPage =
    Observers.forall(o => (mountedOn.get(o) == page and isReady(o)) implies cache.askedOn == page)

  // A component that showed an answer and stays mounted keeps showing one while the shared read holds an answer:
  // no loading flicker on a page move or an own change.
  val stayingComponentsKeepTheirAnswer =
    Observers.forall(o => (wasReady.get(o) and mountedOn.get(o) != Unmounted and cache.status == "ready")
      implies isReady(o))

  // One request per page, plus one per retry and per own change.
  val oneRequestPerPage =
    requestsOnPage <= 1 + retriesOnPage + ownChangesOnPage

  // A request asked on an earlier page never lands: moving cancels it.
  val noEarlierPageRequestInFlight =
    inflight.active implies inflight.askedOn == page

  // After the user's own change, an answer that misses it is never left standing as the latest word.
  val ownChangeIsAsked =
    cache.status != "ready" or cache.includesOwn == ownTotal
      or (inflight.active and inflight.includesOwn == ownTotal)

  val allInvariants = newComponentsSeeOnlyThisPage and stayingComponentsKeepTheirAnswer and oneRequestPerPage
    and noEarlierPageRequestInFlight and ownChangeIsAsked
}
