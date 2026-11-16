# Acceptance brief: story\-6376\-s06\-a\-ratchet\-and\-rename

## Slices

### S1

##### Covers

P1

```text
Run the connector ratchet over today's compiled backend
```

```text
The connector methods found that create a NotSupportedException are exactly the listed ones (today 22: Csv 6, Linear 6, ServiceNow 8, Jira's identity sweep and Azure DevOps' credential conversion), and every listed entry carries a reason
```

```text
A developer sees one test that names every connector method that says it cannot do what its interface asks
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:\:EveryConnectorMethodThatCreatesANotSupportedException\_IsOnTheListWithItsReason

```text
A list holding a different set that the rule also finds; the count of 22 is held by review of the list and the connector-throws-count EXAMINE command, not pinned here, because feature B shrinks the list
```

###### Obligations



##### Covers

P1

```text
Run the rule over made-up connectors that refuse directly, from a private helper, after an await in an async member, inside a lambda, and with an exception derived from NotSupportedException
```

```text
Each is reported with its exact entry: the member itself, the helper Refuse rather than its caller, the async member rather than its state machine, the member that wrote the lambda, and the member throwing the derived exception
```

```text
A refusal hidden in a helper, an async body or a lambda is still caught and named by the method a developer wrote
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:\:AMadeUpConnectorThatRefuses\_IsReportedByName

```text
A rule that folds a lambda written in one overload into another overload of the same name; no made-up connector has overloads
```

###### Obligations







##### Covers

P1

```text
Run the rule over a made-up type shaped like a connector but implementing an interface outside the port, and over a made-up connector throwing InvalidOperationException
```

```text
Neither is reported
```

```text
Types that are no connectors and other exceptions do not make the ratchet fail
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:\:AMadeUpTypeOutsideTheRule\_IsNotReported

```text
A rule that skips a connector type nested inside another type; no made-up case nests a connector
```

###### Obligations




##### Covers

P1

```text
Compare today's backend with the list missing one entry
```

```text
The test fails naming that connector method as not on the list, followed by the fix hint, and asks to delete nothing
```

```text
A developer who adds a refusing connector method is told which one and to remove the interface instead
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:\:AConnectorMethodThatIsNotOnTheList\_FailsNamedWithTheFixHint

```text
A fix hint whose wording drifts from the design text while still naming the method
```

###### Obligations


##### Covers

P1

```text
Compare today's backend with the list holding one extra entry for a method that no longer refuses
```

```text
The test fails naming that entry for deletion and names no newcomer
```

```text
When a connector stops refusing, the developer is told to delete its entry so the list only shrinks
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:\:AListedConnectorMethodThatNoLongerRefuses\_FailsNamedForDeletion

```text
Nothing plausible beyond the shared two-way comparison other ratchets already use
```

###### Obligations


### S2

##### Covers

P2

```text
Read the names of the connector port, of every connector that implements it, and of every Portfolio parameter on their methods, private ones included
```

```text
Nothing says Project for a Portfolio; today it names GetFeaturesForProject on the port and the five connectors and every Portfolio parameter named project, including the CSV connector's private dependency reader
```

```text
A reader of the connector port and the connectors sees Portfolio wherever a Portfolio is meant
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs\:\:ThePortAndEveryConnectorNameAPortfolioAsAPortfolio

```text
A Portfolio passed inside a wrapper type, such as a list of Portfolios, still named project; or a Portfolio constructor parameter named project
```

###### Obligations




##### Covers

P2

```text
Run the same check over a made-up connector that has a private helper taking a Portfolio named project
```

```text
The check names exactly that helper's parameter
```

```text
A connector added later, or a new private helper, cannot bring the wrong word back unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs\:\:AConnectorAddedLaterThatNamesAPortfolioProject\_IsNamedByTheCheck

```text
A helper moved into a separate static class that implements no port interface
```

###### Obligations



##### Covers

P2

```text
Run the same check over a made-up connector whose method is named for a tracker's own project and takes a text named project next to a Portfolio named portfolio
```

```text
The check names nothing on that connector
```

```text
Names for a tracker's own project, such as a Linear project that becomes a Feature, stay as they are
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs\:\:AConnectorThatSaysProjectOnlyForATrackerProject\_PassesTheCheck

```text
A check that ignores every member name and parameter of a connector that does not implement a port member itself
```

###### Obligations



##### Covers

P2

```text
Run the existing backend suite outside the live connector categories before and after the rename, and the fast loop after it
```

```text
The same tests pass before and after
```

```text
Refreshes, validations and answers behave exactly as before the rename
```

##### Disposition

Tested

dotnet test Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj \-\-filter TestCategory\!\=JiraIntegration\&TestCategory\!\=LinearIntegration\&TestCategory\!\=AdoIntegration\&TestCategory\!\=ServiceNowIntegration\&TestCategory\!\=GithubIntegration\; pwsh Lighthouse\.Backend\/Test\-Fast\.ps1

```text
A behaviour change in a path that only the live connector categories reach
```

###### Obligations



## Expectations

### Rows

#### E1

##### Promises

P1

```text
A connector-throws check lists every connector method that throws a not-supported error, per connector folder; adding a new throwing method not on the list makes the check fail naming that method.
```

#### E2

##### Promises

P1

```text
Removing the not-supported throw from a listed connector method (or deleting the method) makes the check fail naming that listed method as stale; the unchanged codebase passes.
```

#### E3

##### Promises

P2

```text
No member or parameter of the connector port or its implementations names a Portfolio as Project; Portfolio terminology appears instead, visible in the public connector surface.
```

#### E4

##### Promises

P2

```text
After the rename, refreshes, validations and responses behave exactly as before: the existing connector backend tests pass with unchanged outcomes and response shapes.
```

## Charter

None.

## Examination

None.
