// One shared read (the permissions summary, or the Teams, Portfolios or licence list) behind many components.
// Observer 1 is the app shell, which stays mounted across page moves; the others belong to one page.
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

  // The answer a component may treat as current: asked on or after the page it mounted on.
  def isReady(o: int): bool =
    mountedOn.get(o) != Unmounted and cache.status == "ready" and cache.askedOn >= mountedOn.get(o)

  action init = all {
    page' = 0,
    cache' = { status: "none", askedOn: Unmounted, includesOwn: 0 },
    inflight' = { active: true, askedOn: 0, includesOwn: 0 },
    mountedOn' = Observers.mapBy(o => if (o == Shell) 0 else Unmounted),
    ownTotal' = 0,
    requestsOnPage' = 1,
    retriesOnPage' = 0,
    ownChangesOnPage' = 0,
  }

  // The user moves to another page: page components unmount, the shared read is asked again once,
  // and a request still in flight from the previous page is cancelled.
  action movesToAnotherPage = all {
    page < MaxPage,
    page' = page + 1,
    inflight' = { active: true, askedOn: page + 1, includesOwn: ownTotal },
    mountedOn' = Observers.mapBy(o => if (o == Shell) mountedOn.get(o) else Unmounted),
    requestsOnPage' = 1,
    retriesOnPage' = 0,
    ownChangesOnPage' = 0,
    cache' = cache,
    ownTotal' = ownTotal,
  }

  // A component mounts on the current page and reads the shared cache; it asks nothing itself.
  action componentMounts(o: int): bool = all {
    mountedOn.get(o) == Unmounted,
    mountedOn' = mountedOn.set(o, page),
    page' = page, cache' = cache, inflight' = inflight, ownTotal' = ownTotal,
    requestsOnPage' = requestsOnPage, retriesOnPage' = retriesOnPage, ownChangesOnPage' = ownChangesOnPage,
  }

  action answerArrives = all {
    inflight.active,
    cache' = { status: "ready", askedOn: inflight.askedOn, includesOwn: inflight.includesOwn },
    inflight' = { ...inflight, active: false },
    page' = page, mountedOn' = mountedOn, ownTotal' = ownTotal,
    requestsOnPage' = requestsOnPage, retriesOnPage' = retriesOnPage, ownChangesOnPage' = ownChangesOnPage,
  }

  action requestFails = all {
    inflight.active,
    cache' = { ...cache, status: "failed" },
    inflight' = { ...inflight, active: false },
    page' = page, mountedOn' = mountedOn, ownTotal' = ownTotal,
    requestsOnPage' = requestsOnPage, retriesOnPage' = retriesOnPage, ownChangesOnPage' = ownChangesOnPage,
  }

  // One retry asks again once, for every component.
  action userRetries = all {
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
    ownChangesOnPage < 2,
    ownTotal' = ownTotal + 1,
    inflight' = { active: true, askedOn: page, includesOwn: ownTotal + 1 },
    requestsOnPage' = requestsOnPage + 1,
    ownChangesOnPage' = ownChangesOnPage + 1,
    page' = page, cache' = cache, mountedOn' = mountedOn, retriesOnPage' = retriesOnPage,
  }

  action step = any {
    movesToAnotherPage,
    nondet o = oneOf(Observers)
    componentMounts(o),
    answerArrives,
    requestFails,
    userRetries,
    ownChangeCompletes,
  }

  // A component of the current page never takes an answer asked on an earlier page as current.
  val pageComponentsSeeOnlyThisPage =
    Observers.forall(o => (o != Shell and isReady(o)) implies cache.askedOn == page)

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

  val allInvariants = pageComponentsSeeOnlyThisPage and oneRequestPerPage
    and noEarlierPageRequestInFlight and ownChangeIsAsked
}
