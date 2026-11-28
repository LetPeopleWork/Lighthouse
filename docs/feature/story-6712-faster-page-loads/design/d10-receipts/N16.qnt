// One shared read (the permissions summary, or the Teams, Portfolios or licence list) behind many components.
// Any component may unmount on a page move, or stay mounted when React keeps it (same type in the same place);
// a page may have no reader at all.
module shared_page_reads {
  pure val MaxPage = 3
  pure val Observers = Set(1, 2, 3)
  pure val Unmounted = -1
  pure val None = -1

  // Last good answer: TanStack keeps data when a later ask fails.
  type Good = { askedOn: int, includesOwn: int }
  // Latest settled ask: its page, whether it carried every own change so far, and whether it failed.
  type Settled = { askedOn: int, includesOwn: int, failed: bool }
  type Request = { active: bool, askedOn: int, includesOwn: int }

  var page: int
  var good: Good
  var settled: Settled
  var inflight: Request
  var mountedOn: int -> int
  var ownTotal: int
  var requestsOnPage: int
  var retriesOnPage: int
  var ownChangesOnPage: int
  // Ghost: readiness of each component in the previous state (no-flicker check only).
  var wasReady: int -> bool

  def mounted(m: int -> int, o: int): bool = m.get(o) != Unmounted

  // Ready: a good answer asked on or after the page the component mounted on. A later failed ask does not remove it.
  def isReady(o: int): bool =
    mounted(mountedOn, o) and good.askedOn != None and good.askedOn >= mountedOn.get(o) and not(settled.failed)

  // Failed: no such good answer, nothing in flight, and the latest ask failed (permissive fallback, loadFailed).
  def isFailed(o: int): bool =
    mounted(mountedOn, o) and not(isReady(o)) and settled.failed and not(inflight.active)

  val readyNow: int -> bool = Observers.mapBy(o => isReady(o))

  def ask(onPage: int): Request = { active: true, askedOn: onPage, includesOwn: ownTotal }

  action init = all {
    page' = 0,
    good' = { askedOn: None, includesOwn: 0 },
    settled' = { askedOn: None, includesOwn: 0, failed: false },
    inflight' = { active: false, askedOn: None, includesOwn: 0 },
    mountedOn' = Observers.mapBy(o => Unmounted),
    ownTotal' = 0,
    requestsOnPage' = 0,
    retriesOnPage' = 0,
    ownChangesOnPage' = 0,
    wasReady' = Observers.mapBy(o => false),
  }

  // The user moves to another page: components unmount unless React keeps them (staying). Invalidation cancels a
  // request still in flight and asks again only when a reader is still mounted (TanStack refetches active queries).
  action movesToAnotherPage(staying: Set[int]): bool = {
    val kept = Observers.mapBy(o => if (staying.contains(o)) mountedOn.get(o) else Unmounted)
    val active = Observers.exists(o => mounted(kept, o))
    all {
      page < MaxPage,
      page' = page + 1,
      mountedOn' = kept,
      inflight' = if (active) ask(page + 1) else { active: false, askedOn: None, includesOwn: 0 },
      requestsOnPage' = if (active) 1 else 0,
      retriesOnPage' = 0,
      ownChangesOnPage' = 0,
      good' = good, settled' = settled, ownTotal' = ownTotal,
      wasReady' = readyNow,
    }
  }

  // A component mounts on the current page. It asks only when nothing is in flight and the latest settled ask was
  // asked on an earlier page (refetchOnMount as a function of askedOnPage); otherwise it reads the shared cache.
  action componentMounts(o: int): bool = {
    val needsAsk = not(inflight.active) and settled.askedOn < page
    all {
      not(mounted(mountedOn, o)),
      mountedOn' = mountedOn.set(o, page),
      inflight' = if (needsAsk) ask(page) else inflight,
      requestsOnPage' = if (needsAsk) requestsOnPage + 1 else requestsOnPage,
      page' = page, good' = good, settled' = settled, ownTotal' = ownTotal,
      retriesOnPage' = retriesOnPage, ownChangesOnPage' = ownChangesOnPage,
      wasReady' = readyNow,
    }
  }

  action answerArrives = all {
    inflight.active,
    good' = { askedOn: inflight.askedOn, includesOwn: inflight.includesOwn },
    settled' = { askedOn: inflight.askedOn, includesOwn: inflight.includesOwn, failed: false },
    inflight' = { ...inflight, active: false },
    page' = page, mountedOn' = mountedOn, ownTotal' = ownTotal,
    requestsOnPage' = requestsOnPage, retriesOnPage' = retriesOnPage, ownChangesOnPage' = ownChangesOnPage,
    wasReady' = readyNow,
  }

  action requestFails = all {
    inflight.active,
    settled' = { askedOn: inflight.askedOn, includesOwn: inflight.includesOwn, failed: true },
    inflight' = { ...inflight, active: false },
    good' = good, page' = page, mountedOn' = mountedOn, ownTotal' = ownTotal,
    requestsOnPage' = requestsOnPage, retriesOnPage' = retriesOnPage, ownChangesOnPage' = ownChangesOnPage,
    wasReady' = readyNow,
  }

  // One retry from a failed component asks again once, for every component.
  action userRetries = all {
    Observers.exists(o => isFailed(o)),
    retriesOnPage < 2,
    inflight' = ask(page),
    requestsOnPage' = requestsOnPage + 1,
    retriesOnPage' = retriesOnPage + 1,
    page' = page, good' = good, settled' = settled, mountedOn' = mountedOn, ownTotal' = ownTotal,
    ownChangesOnPage' = ownChangesOnPage,
    wasReady' = readyNow,
  }

  // The user's own create, rename, delete or grant completes: the shared read is asked again at once, even when no
  // reader is mounted (refetchType all), cancelling a request in flight.
  action ownChangeCompletes = all {
    ownChangesOnPage < 2,
    ownTotal' = ownTotal + 1,
    inflight' = { active: true, askedOn: page, includesOwn: ownTotal + 1 },
    requestsOnPage' = requestsOnPage + 1,
    ownChangesOnPage' = ownChangesOnPage + 1,
    page' = page, good' = good, settled' = settled, mountedOn' = mountedOn, retriesOnPage' = retriesOnPage,
    wasReady' = readyNow,
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
    Observers.forall(o => (mountedOn.get(o) == page and isReady(o)) implies good.askedOn == page)

  // A component that showed an answer and stays mounted keeps showing it: no loading and no fallback on a page
  // move, an own change or a failed ask.
  val stayingComponentsKeepTheirAnswer =
    Observers.forall(o => (wasReady.get(o) and mounted(mountedOn, o)) implies isReady(o))

  // A mounted component with no request in flight is ready or failed, never loading for ever.
  val mountedComponentsNeverHang =
    Observers.forall(o => (mounted(mountedOn, o) and not(inflight.active)) implies (isReady(o) or isFailed(o)))

  // One request per page, plus one per retry and per own change.
  val oneRequestPerPage =
    requestsOnPage <= 1 + retriesOnPage + ownChangesOnPage

  // A request asked on an earlier page never lands: moving cancels it.
  val noEarlierPageRequestInFlight =
    inflight.active implies inflight.askedOn == page

  // After the user's own change, an answer that misses it is never the latest word unless a request carrying it is
  // in flight, the latest ask carrying it failed,
  // or no reader is mounted (the next reader to mount asks again).
  val ownChangeIsAsked =
    good.askedOn == None or good.includesOwn == ownTotal
      or (inflight.active and inflight.includesOwn == ownTotal)
      or (settled.failed and settled.includesOwn == ownTotal)
      or Observers.forall(o => not(mounted(mountedOn, o)))

  val allInvariants = newComponentsSeeOnlyThisPage and stayingComponentsKeepTheirAnswer
    and mountedComponentsNeverHang and oneRequestPerPage and noEarlierPageRequestInFlight and ownChangeIsAsked
}
