# Backend test rules

The backend half of the test-writing skill. NUnit, tests under `Lighthouse.Backend/Lighthouse.Backend.Tests/`. Each rule has a title, a
measured finding in words and one example. The seconds below are Debug builds on one machine, a class run alone, so read them as
proportions and not as promises.

### 1. New backend acceptance scenarios drive the application-service port.

**Finding.** The sampling-window class of the Forecast Reality Check, 51 rows, took 14.0 seconds on a web host and 7.1 seconds at the port
with the same rows and the same ids. Set-up was 115 ms a row on the host and 22 ms a row at the port, and the host's one-time build, 1.8
seconds, sits outside a TRX duration, so a TRX alone makes a host class look faster than it is.

**The three ways out.** When `TestHostUseGuardTest` names a type that starts a host without a reason, it says, and this wording is pinned by a test:
move it to the application-service port (build the service with ServicePortHarness, or with new for up to three collaborators, under Tests/Acceptance/<Feature>/);
list it as a skeleton with its user flow, if it asserts a business outcome that needs the route or the wiring;
or make it a wire contract on WireContractTest in Tests/Contracts/.
A new scenario starts at the port. A skeleton is one or two per user flow, on the list in `TestHostUseGuardTest`; a wire contract covers
the route, status, JSON names, headers and 401/403. A new scenario in a class already on the guard's list goes in a new class at the port too.

**Example.** The port base of the Reality Check builds the real service over a database of the scenario's own, with no host:

```cs
harness = new ServicePortHarness(RegisterWhatTheCheckReads);
using var scope = harness.NewScope();
```

### 2. A host test exists only as a named skeleton or in Contracts

**Finding.** Starting an application host costs hundreds of milliseconds, and a host fixture that is not sharing it pays that again for
every test. The guard `TestHostUseGuardTest` therefore names every type that starts a host on neither list; the listed types are the
ones still to move, and that list only shrinks.

**Example.** The wire contract of the Reality Check sits in `Tests/Contracts/` on the base `WireContractTest`, which builds one host for
all its rows. A skeleton would instead be one entry of `SkeletonsByUserFlow` in `TestHostUseGuardTest`, the type with its user flow:

```cs
public class RealityCheckWireContractTest : WireContractTest
```

### 3. The rule is pinned by a unit test and the wiring by one host test

**Finding.** The three host fixtures of the Reality Check's wire checks took 27.0 seconds for the family on their first split, against
24.4 seconds before: a split that adds host fixtures can be slower, because each fixture builds its own host. Folded onto one wire
contract with 25 rows the family took 23.0 seconds.

**Example.** The rule that five days with finished work are enough to check, and four are not, is asserted at the port as a
`[TestCase(5, true)]` and `[TestCase(4, false)]` pair on `ForecastDataSufficiencyPolicy`'s limit; that the answer's JSON names and
formats are right is asserted once, on the wire contract's host. Changing the limit turns the port rows red and leaves the host row green.

### 4. No shared state across fixtures

**Finding.** The port harness gives each scenario a database file of its own, copied from a schema template (`service-port-<guid>.db`
under the temp folder), and deletes it afterwards; a scenario still got its set-up for 22 ms, so nothing was gained by sharing a
database. A host fixture that keeps one host for several tests resets what differs per test in its per-test set-up.

**Example.** The harness is the unit of isolation: build it per scenario and let it dispose of what it made.

```cs
DatabasePath = Path.Combine(Path.GetTempPath(), $"service-port-{Guid.NewGuid():N}.db");
TestDatabaseTemplate.CopyTo(DatabasePath);
```

### 5. Use a fake clock, never the ambient clock

**Finding.** A reading rather than a timing: a test that asks the real clock for today passes on most hours and fails on the one where
the instance's day and the machine's day differ. `ForecastRealityCheckServiceTest` pins a late evening in UTC that is already tomorrow in
Zurich, which a test on the ambient clock could only meet by luck.

**Example.** `FakeLighthouseClock` takes an instant and a zone:

```cs
var result = Run(new FakeLighthouseClock(lateEveningUtcThatIsAlreadyTomorrowInZurich, TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich")));
```

### 6. A feature's mutation config names `Tests.Acceptance.<Feature>`, because that folder is outside the integration filter

**Finding.** The Stryker configs of the backend leave out `API.Integration` with `FullyQualifiedName!~API.Integration`, so scenarios kept
there never run for a mutant. New port scenarios sit under `Tests/Acceptance/<Feature>/`, outside that filter, and the config still
has to name them, or the code they cover shows as uncovered. The namespace follows the folder (planned: the first feature to put port
scenarios there, as `Tests.Acceptance.<Feature>`).

**Example.** The test-case filter of a feature's config, with the added term last:

```json
"test-case-filter": "(FullyQualifiedName~ForecastRealityCheckServiceTest|FullyQualifiedName~Tests.Acceptance.<Feature>)&FullyQualifiedName!~API.Integration"
```

### 7. Improve the tests you touch

**Finding.** The moved Reality Check class lost nothing: its 51 rows kept their ids, and its line left the list of host users still to
move in the same commit, which the guard asks for: "Its line goes in this commit: take it off the list, which names only types that still start a host."

**Example.** A change that touches rows of a class on `HostUsersStillToMove` moves those rows to the port or to unit level, and takes the
line off the list when the class no longer starts a host. A row that must stay on the host becomes a wire contract or a skeleton.

End of backend.md
