# Acceptance brief: epic\-6577\-system\-wide\-access

## Slices

### S1

##### Covers

P1

```text
A System Admin adds Viewer on all for the group team-readers, then opens the rules list
```

```text
The rule is accepted and the list holds exactly one line: team-readers, Viewer, applies to all Teams and Portfolios
```

```text
The System Admin sees the rule they added, once
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:A\_System\_Admin\_adds\_Viewer\_on\_all\_for\_a\_group\_and\_the\_rules\_list\_shows\_it\_once

```text
The rule is stored as a Team or Portfolio mapping with some unit number instead of applying to all units
```

###### Obligations

None.

##### Covers

P1

```text
A person whose only access is the group team-readers asks to read Team Zenith, a Team first seen after the rule, and a Portfolio
```

```text
All three reads are allowed
```

```text
A group member reads every Team and Portfolio, including ones created later, without being added to any
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:A\_member\_of\_the\_group\_reads\_every\_Team\_and\_Portfolio\_including\_one\_created\_after\_the\_rule

```text
The rule is expanded into rows for the units that exist when it is added and a later unit is covered only because the read check never looks at units
```

###### Obligations

None.

##### Covers

P1

```text
The group member asks for her lists of Teams and Portfolios
```

```text
The Team list holds every Team asked about and the Portfolio list every Portfolio
```

```text
The Overview lists every Team and Portfolio for her
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:A\_members\_lists\_hold\_every\_Team\_and\_Portfolio\_asked\_about

```text
Lists are filtered by a second path that matches the single read checks only for the units named in the test
```

###### Obligations

None.

##### Covers

P1

```text
The group member asks to change a Team and a Portfolio, asks whether she may manage access, and loads her summary
```

```text
Both writes refused, no access management, summary says no System Admin and lists no administered units
```

```text
Reading through the rule changes nothing and gives no admin pages
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:A\_member\_reading\_through\_the\_rule\_changes\_nothing\_and\_does\_not\_administer\_access

```text
Viewer on all grants write on units not asked about here
```

###### Obligations

None.

##### Covers

P1

```text
A person in finance-staff only asks to read Team Zenith, lists Teams and asks to read the Portfolio after the rule exists
```

```text
Every read refused and the list is empty
```

```text
Someone outside the group sees nothing new
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:Someone\_outside\_the\_group\_gains\_nothing\_from\_the\_rule

```text
The rule applies to a group whose value differs only in case
```

###### Obligations

None.

##### Covers

P1

```text
The group member's API key without a limit asks to read Team Zenith and the Portfolio
```

```text
Both reads allowed
```

```text
An unlimited key reads what its owner reads through the rule
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:A\_members\_unlimited\_key\_reads\_what\_the\_member\_reads

```text
The key reads through live group claims it does not carry while the stored group snapshot is ignored
```

###### Obligations

None.

##### Covers

P1

```text
The group member reads Team Zenith, then the System Admin removes the rule and she asks again
```

```text
Allowed before the removal, refused on the next request after it
```

```text
Removing the rule takes the access away at once
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:Removing\_the\_rule\_ends\_the\_members\_reach\_on\_the\_next\_request

```text
Access is cached for longer than the two requests in the test
```

###### Obligations

None.

##### Covers

P1

```text
Viewer on all for team-readers is stored twice; the System Admin lists the rules and removes the one line
```

```text
One line is listed and afterwards no row for the group is left
```

```text
One rule shows once and one Remove ends it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:Removing\_a\_Viewer\_on\_all\_rule\_stored\_twice\_removes\_both\_and\_the\_list\_shows\_it\_once

```text
Removal deletes twins only for the AllUnits scope while twins on another scope stay
```

###### Obligations



##### Covers

P1

```text
Team Zenith has a Viewer and a Team Admin mapping for the same group; the System Admin changes the Team Admin line to Viewer
```

```text
Refused with This group already has this role here.; both lines stay as they were
```

```text
A change that would make two equal rules is refused and says why
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:Changing\_a\_role\_onto\_an\_equal\_rule\_that\_already\_exists\_is\_refused

```text
The refusal is right on update but an equal row is still accepted on a second path such as a differently trimmed group value
```

###### Obligations


##### Covers

P1

```text
The System Admin adds a mapping for every role, every scope, with and without a unit
```

```text
Accepted exactly for System Admin on System without unit, Team Admin on Team, Portfolio Admin on Portfolio, Viewer on Team or Portfolio with a unit, and Viewer on all without unit; a refusal stores no row
```

```text
Only meaningful rules can be added
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:A\_rule\_is\_accepted\_for\_exactly\_the\_valid\_pairs\_of\_role\_and\_scope

```text
A pair is refused by the service but still saved by a later write
```

###### Obligations


##### Covers

P1

```text
With the rule, the group member lists 2 stored Teams and loads her summary, and again with 200 stored Teams
```

```text
Both lists are complete and the database commands sent are the same count for 200 Teams as for 2
```

```text
Large organisations do not pay a lookup per Team
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllListCostTest\.cs\:\:A\_members\_Team\_list\_and\_summary\_cost\_the\_same\_for\_200\_Teams\_as\_for\_2

```text
Cost grows only past 200 Teams through batching
```

###### Obligations


##### Covers

P2

```text
The group member asks whether she may issue a key limited to Viewer on Team Zenith
```

```text
Admitted
```

```text
She can create a key for one Team she reads through the rule
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/LimitedKeyUnderRuleAtThePortTest\.cs\:\:A\_member\_may\_limit\_a\_key\_to\_Viewer\_on\_a\_Team\_she\_reaches\_through\_the\_rule

```text
The issuing check admits Viewer on any unit number, existing or not
```

###### Obligations


##### Covers

P2

```text
Her key limited to Viewer on Team Zenith asks to read Team Zenith, Team Apollo and a Portfolio, to change Team Zenith, and lists Teams
```

```text
Reads Team Zenith only, writes nothing, its Team list is Team Zenith alone
```

```text
A limited key works at its exact scope and nothing beyond
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/LimitedKeyUnderRuleAtThePortTest\.cs\:\:A\_key\_limited\_to\_Viewer\_on\_one\_Team\_reads\_that\_Team\_and\_nothing\_else

```text
The key widens to Portfolios other than the one asked about
```

###### Obligations


##### Covers

P2

```text
The group member asks for a key with Team Admin on Team Zenith
```

```text
Refused with Cannot issue API key with broader scope than your own permissions.
```

```text
A key cannot claim more than its owner has; the message is unchanged
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/LimitedKeyUnderRuleAtThePortTest\.cs\:\:A\_member\_asking\_for\_a\_key\_as\_Team\_Admin\_is\_refused\_with\_the\_existing\_message

```text
Team Admin is refused but Portfolio Admin on a Portfolio is admitted
```

###### Obligations



##### Covers

P2

```text
Her limited key reads Team Zenith, then the System Admin removes the rule and the key asks again
```

```text
Allowed before, refused on the next request
```

```text
Removing the rule stops the key at once
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/LimitedKeyUnderRuleAtThePortTest\.cs\:\:Removing\_the\_rule\_stops\_the\_limited\_key\_on\_its\_next\_request

```text
Coverage is cached per key for longer than the test spans
```

###### Obligations


##### Covers

P2

```text
A key of the outsider limited to Viewer on Team Zenith asks to read it
```

```text
Refused
```

```text
A key never exceeds an owner who reaches nothing
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/LimitedKeyUnderRuleAtThePortTest\.cs\:\:A\_key\_whose\_owner\_reaches\_nothing\_reads\_nothing

```text
A key is covered by a rule of a group its owner left after the snapshot
```

###### Obligations


##### Covers

P1

```text
Access is worked out from a person's own rows and their groups' rules: none; Viewer on all alone; Team Admin on Team A plus the rule; Viewer on Team A plus the rule; every Team or Portfolio role with and without the rule; a System Admin with and without the rule
```

```text
None gives nothing; the rule reads every unit and writes none; Team Admin keeps the write on A and reads elsewhere; Viewer stays Viewer; adding the rule never lowers an answer; the System role stays exactly as the System row says
```

```text
A rule only adds and never makes anyone System Admin
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/Authorization\/EffectiveAccessTest\.cs

```text
Priority ties between Team Admin and Portfolio Admin are broken differently on a unit of the other kind
```

###### Obligations









##### Covers

P1

```text
The compiled production code is scanned for every type that reads the permission or group rule tables
```

```text
The access service is found, and no other type besides the database context reads them
```

```text
No part of Lighthouse decides access around the rules
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessTablesSingleReaderArchUnitTest\.cs\:\:Only\_the\_access\_service\_reads\_the\_permission\_and\_group\_rule\_tables

```text
A type reads the tables through raw SQL text
```

###### Obligations


##### Covers

P1

```text
A System Admin opens Settings, Access with a System Admin group and Viewer on all for team-readers, in an organisation calling Teams Squads and Portfolios Programs
```

```text
Section headed SSO Groups; columns Group, Role, Applies to, Actions; team-readers, Viewer, All Squads and Programs; lh-system-admins, System Admin, Everything
```

```text
The System Admin sees who reaches what in their own words
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsViewerOnAll\.test\.tsx\:\:lists each rule with the group\, its role and what it applies to\, in the organisation\'s words

```text
The Applies to text is right but the Role column shows the stored code name for System Admin elsewhere on the page
```

###### Obligations

None.

##### Covers

P1

```text
The System Admin enters team-readers, picks Viewer on all and adds the mapping; separately adds a System Admin group
```

```text
Viewer on all is sent as Viewer on AllUnits with no unit and one usage event SystemWideAccessGranted with nothing else is reported; the System Admin group is sent as before and reports nothing
```

```text
The rule can be added on the page, and the product learns only that it was used
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsViewerOnAll\.test\.tsx\:\:offers Viewer on all\, adds it for a group and reports that it was granted\, never which group

```text
The event is reported before the server accepted the rule
```

###### Obligations

None.

##### Covers

P1

```text
The System Admin opens Settings, Access while the rules are on their way, and again when they cannot be loaded
```

```text
A spinner where the table goes while loading; Could not load the SSO group rules. Reload the page to try again. in place of the table on failure; no empty table either time
```

```text
A slow or failed load never looks like no rules
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsViewerOnAll\.test\.tsx\:\:shows a spinner where the rules table goes while the rules load

```text
The previous rules stay shown undimmed beside the spinner on a reload
```

###### Obligations

None.

##### Covers

P1

```text
A System Admin group mapping is stored twice; the System Admin lists the rules and removes the one line
```

```text
One line is listed and afterwards no row for the group is left
```

```text
Twins on any scope show once and go together
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:Removing\_a\_System\_Admin\_group\_stored\_twice\_removes\_both\_and\_the\_list\_shows\_it\_once

```text
Twins are collapsed only when their unit is empty, and a Team twin pair stays listed twice
```

###### Obligations



##### Covers

P1

```text
The System Admin adds Viewer on Team Zenith for team-readers, then adds the same mapping again
```

```text
The second add is refused with This group already has this role here. and one row stays stored
```

```text
Adding a rule twice says the group already has it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/ViewerOnAllAtThePortTest\.cs\:\:Adding\_a\_rule\_that\_already\_exists\_is\_refused\_with\_the\_duplicate\_message

```text
Two concurrent adds both pass the check and store twins
```

###### Obligations


##### Covers

P2

```text
Noor holds Team Admin on Team Zenith directly and reads everything through Viewer on all; she holds a Team Admin key and a Viewer key, each limited to Team Zenith, and asks whether she may issue a Team Admin key there
```

```text
She may issue it; the Team Admin key writes Team Zenith and neither reads nor writes Team Apollo; the Viewer key reads Team Zenith and does not write it
```

```text
A wider key works as granted and a narrower key stays narrow
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/LimitedKeyUnderRuleAtThePortTest\.cs\:\:A\_Team\_Admin\_key\_writes\_the\_Team\_its\_owner\_administers\_while\_a\_Viewer\_key\_there\_only\_reads

```text
Key coverage ignores the rule and uses only the owner's direct row on the key's unit
```

###### Obligations



##### Covers

P2

```text
Noor reads only through Viewer on all and holds a stored key limited to Team Admin on Team Zenith; she also asks whether she may issue one
```

```text
The key writes nothing on Team Zenith and issuing is refused
```

```text
A key never claims more than its owner has
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/LimitedKeyUnderRuleAtThePortTest\.cs\:\:A\_Team\_Admin\_key\_of\_an\_owner\_who\_only\_reads\_through\_the\_rule\_writes\_nothing

```text
The key's row is dropped entirely so it also stops reading Team Zenith
```

###### Obligations



##### Covers

P1

```text
The System Admin enters a group, picks System Admin and adds the mapping
```

```text
Sent as System Admin on System with no unit, the rules reload, and no usage event is reported
```

```text
System Admin groups work as before and are not counted as system-wide access
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsViewerOnAll\.test\.tsx\:\:adds a System Admin group as before and reports nothing

```text
The event is reported for a System Admin group added on another path
```

###### Obligations

None.

##### Covers

P1

```text
The System Admin opens Settings, Access when the rules cannot be loaded
```

```text
Could not load the SSO group rules. Reload the page to try again. is shown in place of the table, and no table
```

```text
A failed load never looks like no rules
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsViewerOnAll\.test\.tsx\:\:says the rules could not load\, in place of the table\, when they cannot be loaded

```text
The message shows but an earlier answer stays visible beside it
```

###### Obligations

None.

### S3

##### Covers

P3

```text
A Team Admin opens the Access tab of Team Zenith while the rules call is pending, after it fails, after it answers one Viewer rule for team-readers, and after it answers nothing
```

```text
Pending: a spinner and Checking access to all Teams and Portfolios…, no heading. Failed: a warning saying More people may reach this Team than listed below and to reload, no button. One rule: info notice headed Also reachable through access to all Teams and Portfolios with Viewer: group team-readers and the two sentences. No rule: nothing at all, only after the answer arrived. A Portfolio asks its own rules and says Portfolio
```

```text
The admin is never told nothing reaches the unit when the check is unfinished or failed, and sees which groups reach the unit through the all-units rules
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.test\.tsx

```text
A notice that hides itself while loading or after a failure, or shows an info line instead of the warning
```

###### Obligations


##### Covers

P3

```text
The notice is shown for Team Zenith, then for Team Apollo, and Zenith's answer arrives after the switch
```

```text
Zenith's group never appears under Apollo; Apollo shows its own answer
```

```text
An admin never reads another unit's rules on their unit
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNotice\.test\.tsx

```text
A query keyed by unit kind only, where the last answer to arrive wins
```

###### Obligations

None.

##### Covers

P3

```text
The group list of a Team is rendered read-only for a Team Admin
```

```text
Groups and roles are listed; no Group value field, no Add Group Mapping button, no Actions column and no Remove button
```

```text
A Team Admin is not offered a form whose save is refused
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/ScopedGroupMappingManagerReadOnly\.test\.tsx

```text
Hiding only the add button but keeping Remove buttons or the Actions header
```

###### Obligations

None.

##### Covers

P3

```text
A System Admin opens Settings Access with one user who holds no role of their own
```

```text
The column, the chip No direct role: 1 and the filter Show users with no direct role only say so; the word Unassigned appears nowhere; the filter still narrows to that user
```

```text
Settings says what is true: the user has no direct role, not that they have no access
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsNoDirectRole\.test\.tsx

```text
Renaming the column but leaving Unassigned in the chip or filter
```

###### Obligations

None.

##### Covers

P3

```text
A Team Admin of Zenith, and a Portfolio Admin of Ocean Explorer, ask which rules reach their unit when one Viewer rule exists twice, another group has its own rule, a System Admin group exists and rules for single units exist
```

```text
Distinct role and group pairs only, one line for twin rows, the System Admin group absent, rules of single units absent, nothing listed when no rule reaches the unit, and each line carries only a role and a group
```

```text
The admin reads each rule once, never sees a System Admin group's name, and gets no line to edit
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs

```text
Answering the stored lines without deduplicating, or with a mapping id, or listing the unit's own group mappings
```

###### Obligations






##### Covers

P3

```text
A Team Admin of Zenith, a System Admin, a reader through the Viewer rule, a Team Admin of Apollo and a person with no role each ask for Zenith's rules; the Portfolio twin likewise
```

```text
Only the unit's admin and a System Admin may ask; the Viewer is refused
```

```text
A person who only reads is never shown which groups reach the unit
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/UnitSystemWideRulesAtThePortTest\.cs

```text
Guarding the read with the right to read the unit
```

###### Obligations

None.

##### Covers

P3

```text
A browser asks for the rules of Team 11 and Portfolio 21 under both route versions after a Viewer rule and a System Admin group were stored
```

```text
200 with one line whose members are exactly role and holder, role Viewer, holder team-readers; no id, no scope and no System Admin group
```

```text
The Access tab receives the role and group it prints and nothing it must not show
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Authorization\/SystemWideRulesWireContractTest\.cs

```text
Serialising the stored mapping line with its id and scope
```

###### Obligations


##### Covers

P3

```text
A signed-in Viewer of the unit and an admin of another unit ask for the rules of Team 11 and Portfolio 21 under both route versions
```

```text
403 for each
```

```text
Somebody who only reads, or administers another unit, is turned away
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Authorization\/SystemWideRulesWireContractTest\.cs

```text
A route open to any signed-in person
```

###### Obligations

None.

##### Covers

P3

```text
Every controller action is inspected, including the two new routes
```

```text
Each declares its access
```

```text
No new route is open by omission
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs

```text
A new route with only a class-level Authorize
```

###### Obligations


##### Covers

P3

```text
A Team Admin who is not a System Admin, and then a System Admin, open the Access tab of Team Zenith
```

```text
The Team Admin sees the notice about rules for all Teams and Portfolios and the group list with no form to add a group; the System Admin sees the add form and the notice
```

```text
On the real Access tab a Team Admin is told who else reaches the Team and is not offered a form whose save is refused
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetailAccessTab\.test\.tsx

```text
Mounting the notice but still passing the form to every Team Admin, or hiding the form from System Admins too
```

###### Obligations

None.

##### Covers

P3

```text
A Portfolio Admin who is not a System Admin, and then a System Admin, open the Access tab of Portfolio Ocean Explorer
```

```text
The Portfolio Admin sees the notice and the group list with no add form; the System Admin sees the add form and the notice
```

```text
On the real Access tab a Portfolio Admin is told who else reaches the Portfolio and is not offered a form whose save is refused
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetailAccessTab\.test\.tsx

```text
Mounting the notice on Teams only, or leaving the form for every Portfolio Admin
```

###### Obligations

None.

##### Covers

P3

```text
A browser asks for the rules of an existing Team or Portfolio and of a number no unit ever had, under both route versions
```

```text
The existing unit answers 200 with its rules; the unknown number answers 404, never an empty list
```

```text
A mistyped or deleted unit is not shown as a unit with no rules reaching it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Authorization\/SystemWideRulesWireContractTest\.cs

```text
Answering an empty list for an unknown unit
```

###### Obligations

None.

##### Covers

P3

```text
A Team Admin, and a Portfolio Admin, open the Access tab while the check of their own access is still on its way, and again after it failed
```

```text
The list of groups is shown; no Group value field, no Add Group Mapping button and no Remove button
```

```text
Nobody is offered a form or a Remove button for group mappings until it is known they are a System Admin
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetailAccessTab\.test\.tsx

```text
Showing the form while the person's own access is still loading or after the load failed
```

###### Obligations

None.

##### Covers

P3

```text
A Portfolio Admin opens the Access tab while the check of their own access is on its way, and again after it failed
```

```text
The list of groups is shown; no Group value field, no Add Group Mapping button and no Remove button
```

```text
Nobody is offered a form or a Remove button for group mappings until it is known they are a System Admin
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetailAccessTab\.test\.tsx

```text
Showing the form while the person's own access is still loading or after the load failed
```

###### Obligations

None.

### S4

##### Covers

P4

```text
The instance starts twice on a database holding no switch, a switch on, or a switch off
```

```text
Exactly one switch afterwards: off when none was stored, otherwise unchanged, with the same token after the second start
```

```text
An install, upgrade or restore never opens the instance to everyone, and never turns off what a System Admin turned on
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInSeederTest\.cs

```text
A seeder that rewrites the existing row with its own value on every start; caught by the unchanged-token assertion
```

###### Obligations





##### Covers

P4

```text
On a migrated SQLite and a migrated PostgreSQL database two System Admins read the switch, then both save on that token; on a database with no switch stored a System Admin saves on the token read
```

```text
The first save kept, the second a conflict leaving the kept value; with no row the read is off and the save inserts exactly one row
```

```text
Two System Admins changing the switch at once never undo each other unseen, on either supported database
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInStaleWriteTest\.cs

```text
A token compared in application code before an unconditional update would pass these sequential saves; the true race is the Quint model claim
```

###### Obligations




##### Covers

P4

```text
A Team and a Portfolio Admin open the Access tab while the unit's rules include everyone signed in as Viewer
```

```text
The notice shows the line Viewer: everyone signed in, never as a group, beside group lines, also when it is the only rule
```

```text
A unit's admin sees that everyone signed in reads their unit
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Authorization\/SystemWideRulesNoticeEveryone\.test\.tsx

```text
A notice that drops the group word for every holder; caught by the group line still expected as Viewer: group team-readers
```

###### Obligations

None.

##### Covers

P4

```text
After start-up a System Admin reads the switch; a person with a profile and no group asks to read a Team and a Portfolio; the System Admin turns it on with the token read; the person, their key, a stranger with no profile and a Team Admin ask to read and write; a save on an old token and one with no token; a Team Admin and a person with no role try to save; the switch read fails while the System Admin asks to manage access and the person reads; the switch is turned off; a unit's rules are read; the same with no group claim configured
```

```text
Off with a token by default and a missing switch reads as off; refused before; on: every Team and Portfolio read, including a later one, lists full, writes and access management refused, the key reads as its owner, no-profile principals read nothing, a Team Admin keeps admin on their Team only; the old-token save is a conflict and the tokenless save refused, both leaving the switch as it was; non System Admin saves refused; with the read failing the System Admin still manages access and unit reads are errors; off ends the reach and keeps real roles; a unit's rules name everyone signed in as Viewer only while on; works with no group claim
```

```text
A System Admin opens every Team and Portfolio to everyone signed in by choice, never by upgrade, only for reading, and can close it again without touching anyone's real role
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/EveryoneSignedInAtThePortTest\.cs

```text
A switch reach applied only to people with no unit role, so a Team Admin would not read another Team; caught by the L10 Team Admin row expecting Apollo readable
```

###### Obligations










##### Covers

P4

```text
With everyone signed in turned on, a person with no role reads a Team in a session embedded in another tool; a System Admin removes them; the session makes two more requests
```

```text
Read before removal; both later requests unauthorised; the person is not in the user list again
```

```text
Someone removed from Users stops reading through an embedded session on the very next request, even with everyone signed in on
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/SystemWideAccess\/RemovedPersonEmbedSkeletonTest\.cs

```text
An embed principal that never gets the switch reach at all; caught by the before-removal read expecting 200
```

###### Obligations

None.

##### Covers

P4

```text
A browser reads the switch on both route versions, saves it on the token read, saves again on the old token, saves with no token, and the start-up seeders run; a Team Admin reads and saves
```

```text
200 with exactly everyoneCanView false and concurrencyToken; the save kept; the old-token save 409 with the conflict code; the tokenless save 400 changing nothing; the seeders store one switch off; the Team Admin answered 403 on both
```

```text
Settings and the browser agree on how the switch is read, saved and refused
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Authorization\/EveryoneSignedInWireContractTest\.cs

```text
A route answering 403 for any unknown path: the Team Admin refusal already passes on the current source, so it pins the guard rather than proving it
```

###### Obligations


##### Covers

P4

```text
A System Admin opens Settings > Access while the switch loads, fails to load, is off or on; turns it on and cancels, turns it on and confirms, turns it off, meets a conflict; and an instance with no System Admin yet
```

```text
Loading: disabled under a spinner, never shown off; failure: Could not load this setting. Reload the page to try again.; off with its heading and helper text; turning on asks first naming every Team and Portfolio in the organisation's words and Cancel changes nothing; Turn on saves with the token read, shows on and reports SystemWideAccessGranted once; off saves with no question and reports nothing; a conflict reloads and says someone else changed it; no switch before the first System Admin
```

```text
A System Admin sees the switch's real state, confirms before opening the instance, and is told when someone else changed it
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/Rbac\/RbacSettingsEveryoneSignedIn\.test\.tsx

```text
A switch that saves on the click and shows the dialog afterwards; caught by Cancel asserting no save
```

###### Obligations

None.

##### Covers

P4

```text
Every migration of both databases, including the one adding the switch's table, is read
```

```text
Each one only adds: no drop, rename or narrowing
```

```text
Upgrading never loses data and a release one back still runs, with the switch simply off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ExpandOnlyMigrationGuardTest\.cs

```text
A migration that adds the table and also alters an unrelated column in a way the guard does not classify; the guard owns that boundary
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A System Admin can add Viewer on all for one SSO group, and a member of that group can then read every existing Team and Portfolio, and any Team or Portfolio created afterwards, without being added to any of them.
```

#### E2

##### Promises

P1

```text
A limited API key owned by a member of that group reads the Teams and Portfolios its owner reaches through the rule and its own limit allows, and nothing it was not allowed before.
```

#### E3

##### Promises

P2

```text
A limited API key whose owner reaches a unit only through a system-wide rule works on that unit at the key's exact scope (read-only stays read-only, wider scope works as granted), and never reaches any unit or action beyond the key's limit, even if the owner's rule is wider.
```

#### E4

##### Promises

P3

```text
On a Team or Portfolio Access tab, an Admin of that unit sees a read-only list of the system-wide rules that also reach it, which they cannot edit or remove there; a person who is not an Admin of the unit does not gain this view.
```

#### E5

##### Promises

P3

```text
Where Settings showed Unassigned for a person with no direct role on a unit, it now shows No direct role.
```

#### E6

##### Promises

P4

```text
Everyone signed in is off by default and a fresh or existing install changes no one's access until a System Admin turns it on; turning it on asks for a confirm first, and declining leaves it off.
```

#### E7

##### Promises

P4

```text
After it is confirmed on, every signed-in person with a profile reads every Team and Portfolio, including ones created later; turning it off removes that reach again.
```

#### E8

##### Promises

P5

```text
Admin on all lets members of the chosen group edit settings and members on every Team and Portfolio, including ones created later.
```

#### E9

##### Promises

P5

```text
Holders of Admin on all are still refused every System-Admin-only action (for example system settings and managing system-wide rules).
```

## Charter

None.

## Examination

None.
