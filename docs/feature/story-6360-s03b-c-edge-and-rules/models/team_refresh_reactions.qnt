// One Team refresh pass: TeamDataService saves the items, then (today) calls UpdateTeamMetrics inline
// and publishes TeamDataRefreshed, or (C1) only publishes and a Flow metrics handler updates the metrics.
// The dispatcher runs the handlers one after the other in registration order and swallows a handler's
// exception (DomainEventDispatcher.InvokeHandlerSafely). UpdateTeamMetrics drops the Team's metrics cache
// first and then saves UpdateTime and FeatureWIP, so only the save can fail after the cache is dropped.
// FAILURE_POLICY is how the dispatcher treats a failing handler: "swallow" (the product), "rethrow"
// (the failure leaves the publish and fails the refresh) or "stop" (later handlers are skipped).
module refreshPass {
  const ORDER: List[str]
  const INLINE: bool
  const FAILURE_POLICY: str

  pure val READERS = Set("blocked", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles")

  var pc: str
  var idx: int
  var invalidated: bool
  var metricsSaved: bool
  var metricsFails: bool
  var readerSawStale: bool
  var outcome: str
  var ran: Set[str]

  action init = nondet f = Set(true, false).oneOf() all {
    pc' = "fetch", idx' = 0, invalidated' = false, metricsSaved' = false,
    metricsFails' = f, readerSawStale' = false, outcome' = "running", ran' = Set(),
  }

  action saveItems = all {
    pc == "fetch",
    pc' = if (INLINE) "inlineMetrics" else "publish",
    idx' = idx, invalidated' = invalidated, metricsSaved' = metricsSaved,
    metricsFails' = metricsFails, readerSawStale' = readerSawStale, outcome' = outcome, ran' = ran,
  }

  action inlineMetrics = all {
    pc == "inlineMetrics",
    invalidated' = true,
    metricsSaved' = not(metricsFails),
    pc' = if (metricsFails) "done" else "publish",
    outcome' = if (metricsFails) "failed" else outcome,
    idx' = idx, metricsFails' = metricsFails, readerSawStale' = readerSawStale, ran' = ran,
  }

  pure val ALL_REACTIONS = ORDER.foldl(Set(), (acc, h) => acc.union(Set(h)))

  action dispatchNext = all {
    pc == "publish",
    idx < ORDER.length(),
    val h = ORDER[idx]
    val handlerFailed = (h == "metrics" and metricsFails)
    all {
      invalidated' = (invalidated or h == "metrics"),
      metricsSaved' = (metricsSaved or (h == "metrics" and not(metricsFails))),
      readerSawStale' = (readerSawStale or (READERS.contains(h) and not(invalidated))),
      ran' = ran.union(Set(h)),
      idx' = if (handlerFailed and FAILURE_POLICY == "stop") ORDER.length() else idx + 1,
      pc' = if (handlerFailed and FAILURE_POLICY == "rethrow") "done" else pc,
      outcome' = if (handlerFailed and FAILURE_POLICY == "rethrow") "failed" else outcome,
    },
    metricsFails' = metricsFails,
  }

  action finish = all {
    pc == "publish",
    idx == ORDER.length(),
    pc' = "done", outcome' = "ok",
    idx' = idx, invalidated' = invalidated, metricsSaved' = metricsSaved,
    metricsFails' = metricsFails, readerSawStale' = readerSawStale, ran' = ran,
  }

  action stutter = all {
    pc == "done",
    pc' = pc, idx' = idx, invalidated' = invalidated, metricsSaved' = metricsSaved,
    metricsFails' = metricsFails, readerSawStale' = readerSawStale, outcome' = outcome, ran' = ran,
  }

  action step = any { saveItems, inlineMetrics, dispatchNext, finish, stutter }

  // A snapshot recorder never reads the Team's metrics from a cache the refresh has not yet dropped.
  val ReadersNeverSeeStaleMetrics = not(readerSawStale)
  // Accepted behaviour, today and after C1: a refresh whose metrics save succeeds ends ok with the save kept.
  val SuccessfulSaveIsKept = (pc == "done" and not(metricsFails)) implies (outcome == "ok" and metricsSaved)
  // Accepted behaviour after C1: a failed metrics save leaves the refresh ok, the cache dropped, UpdateTime and
  // FeatureWIP unsaved, and every TeamDataRefreshed reaction run.
  val MetricsFailureLeavesRefreshOkAndSaveUndone =
    (pc == "done" and metricsFails) implies (outcome == "ok" and not(metricsSaved) and invalidated and ran == ALL_REACTIONS)

  // Today's failure behaviour only, kept to document the difference C1 makes; not a target for C1.
  // A refresh that ends "ok" has saved the Team's UpdateTime and FeatureWIP.
  val OkRefreshHasSavedMetrics = (outcome == "ok") implies metricsSaved
  // A failed metrics save fails the refresh, and no TeamDataRefreshed reaction runs.
  val MetricsFailureFailsTheRefresh = (pc == "done" and metricsFails) implies (outcome == "failed" and ran == Set())
}

module today {
  import refreshPass(ORDER = ["memo", "forecast", "blocked", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = true, FAILURE_POLICY = "swallow").*
}

module c1 {
  import refreshPass(ORDER = ["metrics", "memo", "forecast", "blocked", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = false, FAILURE_POLICY = "swallow").*
}

// Behavioural mutant: the metrics handler registered after the first snapshot recorder.
module c1Swapped {
  import refreshPass(ORDER = ["memo", "forecast", "blocked", "metrics", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = false, FAILURE_POLICY = "swallow").*
}

// Behavioural mutant: today's inline call removed with no handler registered.
module c1Dropped {
  import refreshPass(ORDER = ["memo", "forecast", "blocked", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = false, FAILURE_POLICY = "swallow").*
}

// Behavioural mutant: the dispatcher lets the metrics handler's failure leave the publish.
module c1Rethrow {
  import refreshPass(ORDER = ["metrics", "memo", "forecast", "blocked", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = false, FAILURE_POLICY = "rethrow").*
}

// Behavioural mutant: the dispatcher skips the remaining reactions after a failing handler.
module c1StopsOnFailure {
  import refreshPass(ORDER = ["metrics", "memo", "forecast", "blocked", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = false, FAILURE_POLICY = "stop").*
}
