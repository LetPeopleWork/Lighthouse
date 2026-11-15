# Acceptance brief: story\-6360\-s03b\-a\-rules\-first

## Slices

### S1

##### Covers

P1

```text
A made-up connector reaches a made-up Work Items type placed in the Work Items module; a second connector reaches a namesake just outside the module, and a third reaches only a plain value
```

```text
The connector leaf rule's own Work Items pattern names the connector reaching into the module, and names neither the namesake reader nor the value reader
```

```text
A connector that reaches into Work Items after the move is still refused by the leaf rule
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:\:WorkItemsRulesSeeTheModuleArchUnitTest\.AConnectorReachingATypeUnderTheModule\_IsCaughtByTheConnectorLeafRule

```text
The pattern also took in the old Dependencies and Parents folders, which the real connectors call; the run over the real code catches that, not this test
```

###### Obligations

None.

##### Covers

P1

```text
A made-up dependency type in the Work Items module reaches the forecast; a compliant twin there reads only a value; a namesake just outside the module reaches the forecast
```

```text
The dependency rule's own namespace key names the type in the module reaching the forecast, and names neither the twin nor the namesake
```

```text
A dependency decision that moves into Work Items still may not reach into the simulation
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:\:WorkItemsRulesSeeTheModuleArchUnitTest\.ADependencyTypeUnderTheModuleReachingTheForecast\_IsCaughtByTheForecastIsolationRule

```text
The key took in every module whose name does not start with WorkItems, for example the whole Modules tree minus the namesake; the oracle checks only the Work Items module and its namesake
```

###### Obligations

None.

##### Covers

P1

```text
A made-up forecast step reaches a made-up Work Items type in the module; a second reaches a namesake just outside the module
```

```text
The dependency rule's own namespace key, used as the forbidden side, names the step reaching into the module and not the one reaching the namesake
```

```text
The forecast still may not reach dependency types once they live in Work Items
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:\:WorkItemsRulesSeeTheModuleArchUnitTest\.TheForecastReachingADependencyTypeUnderTheModule\_IsCaughtByTheForecastIsolationRule

```text
The rule kept the key but stopped using it on its forbidden side; this test reads the key, not the rule body
```

###### Obligations

None.

##### Covers

P1

```text
A made-up file under the Work Items module folder, one in the old Dependencies folder and one in a namesake folder just outside the module are judged by the folder lists of the dependency scans: what the dependency work added, and where a dependency source may be recorded
```

```text
Each list takes in a folder under Modules/WorkItems, keeps the old Dependencies folder, and does not take in the namesake folder
```

```text
The dependency word and licence scans keep reading the dependency files after they move into Work Items
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:\:WorkItemsRulesSeeTheModuleArchUnitTest\.TheDependencyFolderScans\_TakeInAFolderUnderTheModuleBesideTheOldOne

```text
The module folder is the whole module or a whole layer, which would later also scan or exempt the Work Item service; the oracle does not pin the subfolder
```

###### Obligations

None.

##### Covers

P1

```text
The file keys of the one-decision rules for the reconciler, the honour policy and the decision are read from the rules and looked up in the production source
```

```text
Each key is the bare file name and exactly one production file carries it, so the rule finds the file wherever it lives
```

```text
Moving the reconciler, honour policy or decision into Work Items does not switch the one-decision rules off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:\:WorkItemsRulesSeeTheModuleArchUnitTest\.TheOneDecisionFiles\_AreFoundByTheirNameWhereverTheyLive

```text
The keys are bare names but the rules still compare them with full paths; the run over the real code catches that as no longer calls, not this test
```

###### Obligations

None.

##### Covers

P1

```text
The tracker history cutoff list is read from the calendar-day rule and its Work Item service entries are looked up in the production source
```

```text
Both Work Item service window cutoffs name the file by its name alone, and exactly one production file carries that name
```

```text
Moving the Work Item service into Work Items does not make its history cutoffs report the file as gone
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:\:WorkItemsRulesSeeTheModuleArchUnitTest\.TheWorkItemServiceHistoryCutoffs\_FindTheirFileByItsNameWhereverItLives

```text
The entries hold the bare name but the rule still joins it to a fixed folder; the run over the real code catches that as file no longer exists, not this test
```

###### Obligations

None.

### S3

##### Covers

P3

```text
A Team reading a fixed CSV upload of four Work Items, one of them in the blocked state, is refreshed for the first time at a fixed instant
```

```text
The four Work Items are stored with their names, types, states, dates, parents and tags as written out; no moves; one open blocked period for the waiting item from the refresh instant; announcements in order: that item blocked, then the Team refreshed; a full sync of 4
```

```text
The Team's items, blocked history and the reactions to a refresh are the same after the code moves
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:\:A\_Teams\_first\_refresh\_stores\_its\_Work\_Items\_and\_opens\_the\_blocked\_period\_of\_the\_waiting\_one

```text
A reaction other than the blocked-period capture stops running, since only the blocked periods are observed among the reactions
```

###### Obligations

None.

##### Covers

P3

```text
A day later the same Team reads an upload where one item started, one finished, the blocked one moved on, one left and one is new, and is refreshed
```

```text
Four stored items as written out, the departed one gone; three moves dated at the second refresh; the blocked period closed at the second refresh; announcements in order: three moves, the unblock, the Team refreshed
```

```text
Cycle times, blocked history and removals after a refresh stay the same after the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:\:A\_Teams\_next\_refresh\_records\_the\_moves\_closes\_the\_blocked\_period\_and\_drops\_what\_left\_the\_upload

```text
A change that alters only the moment the metrics renew inside the refresh, since the Team's refresh time is not read
```

###### Obligations

None.

##### Covers

P3

```text
A Portfolio reading a fixed CSV upload of three Features is refreshed for the first time
```

```text
Three Features stored with states, dates, parent and size as written out; no moves; no blocked period; no announcements; a full sync of 3
```

```text
A Portfolio's Features and their parent placement are the same after the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:\:A\_Portfolios\_first\_refresh\_stores\_its\_Features\_under\_their\_parents\_and\_announces\_nothing

```text
A changed remaining-work split across Teams, since remaining work is not part of the promise and no Team works on these Features
```

###### Obligations

None.

##### Covers

P3

```text
A day later the Portfolio reads an upload where one Feature now waits and one has started, and is refreshed
```

```text
The new states stored as written out; still no Feature moves; one open blocked period for the waiting Feature from the second refresh; one announcement: that Feature blocked in the Portfolio for Waiting
```

```text
A Feature's blocked history after a refresh is the same after the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs\:\:A\_Portfolios\_next\_refresh\_stores\_the\_new\_states\_without\_moves\_and\_opens\_the\_blocked\_period\_of\_the\_waiting\_Feature

```text
A Feature unblock path that changed, since no Feature leaves the blocked state in this scenario; the Work Item unblock covers that path for Work Items only
```

###### Obligations

None.

##### Covers

P3

```text
On demo data, every Team and Portfolio is refreshed on the pinned base revision and on the current revision, and the Work Item, Feature list, dependency and blocked answers are captured on both
```

```text
One line per answer kind and a last line VERDICT IDENTICAL; a failure when no answer was captured or any answer was not a success
```

```text
A user can see from outside that refresh answers did not change
```

##### Disposition

Tested

bash Scripts\/examine\/bin\/refresh\-answers\-compare

```text
A difference in a time-stamped field that the comparison masks
```

###### Obligations

None.

##### Covers

P3

```text
This checkout's backend on a fresh database loads the smallest demo scenario with Teams and a Team is refreshed through the public API
```

```text
One line per Work Items reaction seen in public answers such as refresh history and blocked history, a reaction with no public observation named as not observable, and a last line VERDICT OBSERVED
```

```text
A user can watch the Work Items reactions fire after a refresh
```

##### Disposition

Tested

bash Scripts\/examine\/bin\/refresh\-reactions\-show

```text
A reaction named not observable that stopped running, since nothing public shows it
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
When a made-up type is placed in the Work Items module folder (Modules/WorkItems), every architecture rule that applies to Work Items namespaces, folders, paths or types flags it, just as it would for the existing Work Items locations.
```

#### E2

##### Promises

P2

```text
A test fixes the list of Work Items types that move, derived from the compiled code, and that test fails when a listed type is missing from the code and when a Work Items type that is not on the list appears.
```

#### E3

##### Promises

P3

```text
Asking the service port to refresh gives the same items, transitions, blocked periods and events before and after the move, so a user sees no change in their data.
```

#### E4

##### Promises

P3

```text
Public commands let a user capture the refresh answers and watch the Work Items reactions fire on demo data, so the before and after can be compared from outside.
```

## Charter

None.

## Examination

None.
