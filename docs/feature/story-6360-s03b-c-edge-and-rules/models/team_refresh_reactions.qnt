// One Team refresh pass: TeamDataService saves the items, then (today) calls UpdateTeamMetrics inline
// and publishes TeamDataRefreshed, or (C1) only publishes and a Flow metrics handler updates the metrics.
// The dispatcher runs the handlers one after the other in registration order and swallows a handler's
// exception (DomainEventDispatcher.InvokeHandlerSafely). UpdateTeamMetrics drops the Team's metrics cache
// first and then saves UpdateTime and FeatureWIP, so only the save can fail after the cache is dropped.
module refreshPass {
  const ORDER: List[str]
  const INLINE: bool

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

  action dispatchNext = all {
    pc == "publish",
    idx < ORDER.length(),
    val h = ORDER[idx]
    all {
      invalidated' = (invalidated or h == "metrics"),
      metricsSaved' = (metricsSaved or (h == "metrics" and not(metricsFails))),
      readerSawStale' = (readerSawStale or (READERS.contains(h) and not(invalidated))),
      ran' = ran.union(Set(h)),
    },
    idx' = idx + 1,
    pc' = pc, metricsFails' = metricsFails, outcome' = outcome,
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
  // A refresh that ends "ok" has saved the Team's UpdateTime and FeatureWIP.
  val OkRefreshHasSavedMetrics = (outcome == "ok") implies metricsSaved
  // A failed metrics save fails the refresh, and no TeamDataRefreshed reaction runs.
  val MetricsFailureFailsTheRefresh = (pc == "done" and metricsFails) implies (outcome == "failed" and ran == Set())
}

module today {
  import refreshPass(ORDER = ["memo", "forecast", "blocked", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = true).*
}

module c1 {
  import refreshPass(ORDER = ["metrics", "memo", "forecast", "blocked", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = false).*
}

// Behavioural mutant: the metrics handler registered after the first snapshot recorder.
module c1Swapped {
  import refreshPass(ORDER = ["memo", "forecast", "blocked", "metrics", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = false).*
}

// Behavioural mutant: today's inline call removed with no handler registered.
module c1Dropped {
  import refreshPass(ORDER = ["memo", "forecast", "blocked", "percentiles", "processBehavior", "demoBlocked", "demoPercentiles"], INLINE = false).*
}
