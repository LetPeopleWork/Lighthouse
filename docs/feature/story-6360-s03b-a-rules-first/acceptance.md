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
Made-up files in the Dependencies sub-folders of the Work Items module's Domain and Application layers, in the old Dependencies folder, directly in the module and its layers outside any Dependencies sub-folder, and in a namesake module, are judged by the folder lists of the dependency scans: what the dependency work added, and where a dependency source may be recorded
```

```text
Each list takes in a module Dependencies sub-folder and keeps the old Dependencies folder, and takes in no file of the module outside the Dependencies sub-folders and none of the namesake module
```

```text
The dependency word and licence scans keep reading the dependency files after they move into Work Items, and do not start scanning or exempting the Work Item service
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:\:WorkItemsRulesSeeTheModuleArchUnitTest\.TheDependencyFolderScans\_TakeInTheModulesDependenciesFoldersOnly\_BesideTheOldOne

```text
A key names a deeper or oddly named folder below a Dependencies sub-folder, or names only one of the two layers while the dependency files land in the other; feature B's real-code scans catch a file left unread, not this test
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

### S2

##### Covers

P2

```text
Read the compiled Lighthouse code and collect every top-level type in a Work Items namespace, old or new, plus the six Work Items reactions and the four refresh-log candidates found by name
```

```text
The collected types are exactly the 41 types listed to move plus the 5 listed to stay, compared by name both ways
```

```text
The maintainer has one fixed list of what moves into Modules/WorkItems and what stays behind, measured from the code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:\:TheWorkItemsTypesInTheCompiledCode\_AreExactlyTheTypesListedToMoveOrToStay

```text
a type listed on both lists at once; caught by NoTypeIsListedBothToMoveAndToStay
```

###### Obligations

None.

##### Covers

P2

```text
Add a made-up type under Modules.WorkItems, another under the old Work Items namespace, a namesake in Modules.WorkItemsMembershipFixtures and a nested type, then compare with the lists
```

```text
Both made-up types are named as not on the list; the namesake and the nested type are not named; no listed type is reported missing
```

```text
A type that joins Work Items during the move without being listed fails the test by name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:\:AMadeUpTypeInAWorkItemsNamespace\_IsNamedAsUnlisted\_AndANamesakeOutsideIsNot

```text
a made-up type that only another assembly holds; the production collection itself is unchanged by it
```

###### Obligations

None.

##### Covers

P2

```text
Rename a listed type in the collected set, as a move that also renames would
```

```text
The old name is named as listed but missing and the new name as not on the list
```

```text
A type that disappears or is renamed during the move fails the test by name until the list follows
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:\:AListedTypeRenamedInTheCode\_IsNamedAsMissing\_AndItsNewNameAsUnlisted

```text
a move that keeps the short name and changes only the namespace; correct by design, the lists hold before and after the move
```

###### Obligations

None.

##### Covers

P2

```text
Judge each of the four refresh-log candidates by its compiled outbound edges and, for an interface, by whether its only implementation stays
```

```text
RefreshLogService and IRefreshLogService reach only the kernel, repositories, settings, logging and Work Items and are on Moves; OrphanedFeatureCleanupService reaches the database context, EF Core and the service scope factory and is on Stays; IOrphanedFeatureCleanupService reaches nothing outside but its only implementation stays, so it is on Stays with the same owner
```

```text
Which refresh-log services move is decided by what their compiled code reaches, a port stays with an implementation that stays, and a change in either fails the test naming the cause
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:\:ARefreshLogCandidate\_MovesExactlyWhenItAndTheImplementationsOfItReachOnlyWhatWorkItemsMayReach

```text
a candidate reaching a forbidden type only in a catch clause, an event or a named attribute argument, which the IL reader does not see; the failure text says so
```

###### Obligations

None.

##### Covers

P2

```text
Read every entry of the Stays list
```

```text
Each entry names the context that keeps the type, and no type is on both lists
```

```text
Every Work Items type left behind has a named owner who will move it later
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:\:EveryTypeThatStays\_NamesTheContextThatKeepsIt

```text
an owner text that names no real context, such as one word; judged by review
```

###### Obligations

None.

##### Covers

P2

```text
Judge a made-up port with clean edges whose only implementation reads the database context, and a made-up port whose implementation reaches nothing outside
```

```text
The first port reaches nothing outside itself, is held back by its implementation by name and is not admitted; the second port is admitted
```

```text
An interface never moves into Modules/WorkItems ahead of the implementation it belongs to
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:\:AnInterfaceWithCleanEdges\_StaysWhenItsOnlyImplementationStays\_AndMovesWhenItsImplementationMoves

```text
an interface with two implementations of which only one stays; it is admitted, because the amended rule holds a port back only when its only implementation stays
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

### E1

met

#### Observations

```text
architecture-rules WorkItems: 17/17 pass incl. connector-leaf rule, forecast-isolation rule (both directions), dependency-folder scans, one-decision files and history-cutoff files found by name wherever they live, made-up type in Work Items namespace named as unlisted with outside namesake not. Limit: filter shows only these rule families; full rule list not enumerated by me.
```

### E2

met

#### Observations

```text
architecture-rules Membership: 10/10 pass: TheWorkItemsTypesInTheCompiledCode_AreExactlyTheTypesListedToMoveOrToStay, made-up type named unlisted, renamed listed type named missing plus new name unlisted, no type both Moves and Stays, every Stays names owner. Limit: failure paths seen only via passing tests of those names, not a hand-made counterexample.
```

### E3

met

#### Observations

```text
refresh-answers-compare (base 8a785450c vs HEAD eae00ff27, demo data): 297 answers over 9 kinds, 0 different each, VERDICT: IDENTICAL. Covers work-items, blocked today/a week ago/count history, in-progress, finished, feature-list, dependencies. Transitions and events not read directly; service-port literal-equality test not probed by me.
```

### E4

met

#### Observations

```text
refresh-answers-compare (base 8a785450c vs HEAD eae00ff27, demo data): 297 answers over 9 kinds, 0 different each, VERDICT: IDENTICAL. Covers work-items, blocked today/a week ago/count history, in-progress, finished, feature-list, dependencies. Transitions and events not read directly; service-port literal-equality test not probed by me.
```

```text
refresh-reactions-show: VERDICT OBSERVED; 3 observable reactions seen (team refresh recorded in history; changed download refreshes teams; deleted team clears history); 4 blocked-period reactions reported NOT OBSERVABLE through public API with stated reason (covered at service port). One line per reaction printed. Post-move only, not compared to pre-move.
```
