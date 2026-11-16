# Acceptance brief: story\-6376\-s06\-a\-ratchet\-and\-rename

## Slices

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
