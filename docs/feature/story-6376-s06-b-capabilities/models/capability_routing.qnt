// Connector capability routing: the type-tested route after the split must
// equal the flag-driven route before it, for every connector, request and
// state of Jira's process-wide deployment cache.
module capability_routing {
  type Connector = Csv | Linear | ServiceNow | Jira | AzureDevOps
  type Request = WriteBack | ReadPortfolioFeatures | ReadParentFeatures | TeamScan | PortfolioScan | ParentScan
  type Outcome = Served | FailsNotSupported | SweepPath | FullPath

  pure val CONNECTORS = Set(Csv, Linear, ServiceNow, Jira, AzureDevOps)
  pure val REQUESTS = Set(WriteBack, ReadPortfolioFeatures, ReadParentFeatures, TeamScan, PortfolioScan, ParentScan)

  // Before: every connector claims every member; unsupported members throw.
  pure def oldProbe(c: Connector, deploymentKnown: bool): bool =
    match c { | Jira => deploymentKnown | AzureDevOps => true | _ => false }
  pure def oldWriteBackThrows(c: Connector): bool =
    match c { | Jira => false | AzureDevOps => false | _ => true }
  pure def oldFeatureReadThrows(c: Connector): bool =
    match c { | ServiceNow => true | _ => false }
  pure def oldRoute(c: Connector, r: Request, deploymentKnown: bool): Outcome =
    match r {
      | WriteBack => if (oldWriteBackThrows(c)) FailsNotSupported else Served
      | ReadPortfolioFeatures => if (oldFeatureReadThrows(c)) FailsNotSupported else Served
      | ReadParentFeatures => if (oldFeatureReadThrows(c)) FailsNotSupported else Served
      | _ => if (oldProbe(c, deploymentKnown)) SweepPath else FullPath
    }

  // After: the type says what a connector can ever do; the probe lives on the
  // incremental capability and says what it can do for this connection now.
  pure def isWriteBackTarget(c: Connector): bool = Set(Jira, AzureDevOps).contains(c)
  pure def isFeatureSource(c: Connector): bool = Set(Jira, AzureDevOps, Linear, Csv).contains(c)
  pure def isIncrementalSource(c: Connector): bool = Set(Jira, AzureDevOps).contains(c)
  pure def capabilityProbe(c: Connector, deploymentKnown: bool): bool =
    match c { | Jira => deploymentKnown | _ => true }
  pure def newRoute(c: Connector, r: Request, deploymentKnown: bool): Outcome =
    match r {
      | WriteBack => if (isWriteBackTarget(c)) Served else FailsNotSupported
      | ReadPortfolioFeatures => if (isFeatureSource(c)) Served else FailsNotSupported
      | ReadParentFeatures => if (isFeatureSource(c)) Served else FailsNotSupported
      | _ => if (isIncrementalSource(c) and capabilityProbe(c, deploymentKnown)) SweepPath else FullPath
    }

  var connector: Connector
  var request: Request
  var jiraDeploymentKnown: bool

  action init = {
    nondet c = oneOf(CONNECTORS)
    nondet r = oneOf(REQUESTS)
    all { connector' = c,
    request' = r,
    jiraDeploymentKnown' = false, }
  }

  // A refresh asks; Jira's deployment may become known once and stays known.
  action step = {
    nondet c = oneOf(CONNECTORS)
    nondet r = oneOf(REQUESTS)
    nondet learned = oneOf(Set(true, false))
    all { connector' = c,
    request' = r,
    jiraDeploymentKnown' = (jiraDeploymentKnown or learned), }
  }

  val sameRoute = newRoute(connector, request, jiraDeploymentKnown) == oldRoute(connector, request, jiraDeploymentKnown)
  // Inhabitation of the new capabilities: a sweep is reachable only through one.
  val sweepOnlyThroughCapability = newRoute(connector, request, jiraDeploymentKnown) == SweepPath implies isIncrementalSource(connector)
  val noThrowFromClaimedMember =
    (request == WriteBack and isWriteBackTarget(connector) implies newRoute(connector, request, jiraDeploymentKnown) == Served)
    and ((request == ReadPortfolioFeatures or request == ReadParentFeatures) and isFeatureSource(connector) implies newRoute(connector, request, jiraDeploymentKnown) == Served)
  val sweepReachable = newRoute(connector, request, jiraDeploymentKnown) != SweepPath
}
