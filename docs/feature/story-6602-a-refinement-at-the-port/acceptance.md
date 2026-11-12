# Acceptance brief: story\-6602\-a\-refinement\-at\-the\-port

## Slices

### S1

##### Covers

P1

P2

```text
Run the yardstick class at the service port: a Team with a whole SLE, without one, with half of one, with no finished work, with slow work finished outside, on the first day of and the day before the Throughput window, and two voters reading the same Team, today 7 October 2026
```

```text
Its 10 rows pass under their unchanged names without a web host: the SLE of 7 days at 85%, the fallback of 12 days at 85% (60 days when slow work finished on the window's first day), and no number when nothing finished inside the window; both voters see the same fallback
```

```text
A maintainer sees the yardstick rules proven at the port, faster, with no test lost
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice10SleYardstickScenarios\.cs

```text
The finished-work Given counted days back from a fixed 7 October while the clock read another day, so the window edges shift together and the edge rows still agree; held off by setting today explicitly in every Given and by the deliberate break of a one-day-wider window turning the day-before row red
```

###### Obligations



##### Covers

P2

```text
On the wire, save a Team's SLE through the Team settings (set 75% within 10 days, then clear it) and read the Refinement tab before and after; also read the tab of a Team with no SLE and nothing finished
```

```text
The 2 rows Setting_an_SLE_replaces_the_fallback_on_the_next_read and Clearing_the_SLE_brings_the_fallback_back_on_the_next_read pass under their names in the wire class: the next read answers the saved SLE or the 12-day fallback, and the same Work Items stay listed; the new row shows the source as the text Unavailable with days and probability sent as null
```

```text
A browser keeps reading the yardstick names and texts it reads today, and an SLE an admin saves is what voters answer against next
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Yardstick\.cs

```text
The yardstick days or probability left out of the answer when there is no number instead of sent as null; held off by asserting the JSON kind Null, not a null read of a missing member
```

###### Obligations


##### Covers

P1

```text
Run the host-use guard after the yardstick class moves
```

```text
The guard passes and its list of host users still to move no longer names the yardstick class; the list gains no line
```

```text
A maintainer sees one class fewer starting a web host
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

```text
The class line stays on the list while the class no longer starts a host; the guard asks for the line to go in the same commit
```

###### Obligations


##### Covers

P3

```text
Run the port-Givens guard over the port classes and its planted classes, including a planted port class that adds to a Team's To Do states, sets a nested setting reachable from a Team, or writes a guarded table through the database context
```

```text
No real port class writes Team set-up outside the port builder, and each planted case, the To Do states addition among them, is reported by the guard
```

```text
A maintainer knows a port scenario cannot pass on a Team no settings save would produce
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs

```text
The guard reads only direct setters on Team and misses collection mutators and nested setters; held off by one planted case per gap
```

###### Obligations



##### Covers

P3

```text
Run the settings-parity rows of the Refinement wire contract: a Team set up by the port builder against one saved through the settings form, compared on SLE probability and range, states and Throughput history as well as how it refines
```

```text
The parity rows pass and compare every public property of those Team fields, so a field the builder sets otherwise than the save turns them red
```

```text
A maintainer knows the port builder stores what the settings save stores
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs

```text
Comparison through a JSON serialisation that skips ignored members; held off by a public-property dump
```

###### Obligations

None.

##### Covers

P2

```text
Compare the sorted test ids of the yardstick class and the wire class after the move with the ids recorded before it
```

```text
The only differences are the 2 rows that moved to the wire class and the 1 new wire row
```

```text
A maintainer sees no test lost or renamed by the move
```

##### Disposition

NotTested

```text
Checked by comparing the run's id list with the recorded before list (design verification step 2), not by a test
```

crafter and reviewer

### S2

##### Covers

P2

```text
On an instance without sign-in, Gravity finishes two Work Items a day and refines on Tuesdays; the Refinement tab is read over HTTP and the manual forecast route is asked for the day seven working days ahead
```

```text
The tab's low end equals the manual forecast's median, the range is 14 to 14 over 7 working days, and the cycle runs from the next Tuesday to the Tuesday after it
```

```text
The number on the Refinement tab is the same forecast the Team sees on its Forecasts tab for the same working days
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Contracts\.Refinement\.RefinementWithoutSignInWireContractTest\.The\_range\_uses\_the\_same\_horizon\_as\_the\_manual\_forecast

```text
both routes used the same wrong horizon of seven calendar days on a Team with no blackout days, where calendar and working days agree
```

###### Obligations

None.

##### Covers

P2

```text
On an instance without sign-in, one Team has finished work on only four days and one Team has chosen no state to refine in; both Refinement tabs are read over HTTP
```

```text
The need's reason is sent as the strings InsufficientData and NoRefinementStates
```

```text
The browser and the clients can say why there is no number
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Contracts\.Refinement\.RefinementWithoutSignInWireContractTest\.A\_need\_without\_a\_number\_names\_its\_reason\_as\_text

```text
a third reason the browser reads were renamed on the wire; NoCadence is held by the existing wire row
```

###### Obligations

None.

##### Covers

P1

```text
The host-use guard reads the test assembly after the need-and-verdict class moved to the service port
```

```text
The class starts no web host and its line is gone from the list of host users still to move; the list gains no line
```

```text
The suite runs this class without paying for a web host
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.TestHostUseGuardTest

```text
another hosted class kept its line, which later slices take off
```

###### Obligations


##### Covers

P3

```text
The settings stand-in of the port fixture is compared with a newly seeded database, and the port fixture's registrations are compared with the host's
```

```text
Every answered setting equals the seeded one, a read the stand-in does not answer throws instead of returning a default, sign-in off agrees with the host's answer on an instance without sign-in, and the sizing log commands are registered with the host's lifetime
```

```text
A port scenario cannot pass on a setting, sign-in mode or lifetime no real instance has
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Contracts\.Refinement\.RefinementWithoutSignInWireContractTest\.Every\_setting\_a\_port\_scenario\_is\_given\_is\_what\_a\_seeded\_database\_holds

```text
the stand-in answered a setting the seeded database also leaves at its default, so a strict throw is the only signal
```

###### Obligations

None.

##### Covers

P1

P2

```text
Gravity has two ready Work Items and refines on Thursdays; the Team is likely to pull a scripted low and high over its cycle; the coach opens the Refinement tab at the service port
```

```text
Low 3 high 8 gives Below, 2-8 and 1-2 give In, 0-1 gives Above: both ends of the range count as in range
```

```text
The coach is told to refine more, to stop, or that the backlog is right, with the ends of the range counting as enough
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.API\.Integration\.Refinement\.Slice05NeedAndVerdictTest\.The\_verdict\_compares\_the\_ready\_count\_with\_both\_ends\_of\_the\_range

```text
the verdict were right only at these four scripted pairs while reading the range at a likelihood other than the median and 85 percent that happens to give the same counts
```

###### Obligations

None.

##### Covers

P1

P2

```text
On a Team without stage rules three voters say Yes on one Work Item through the sizing log commands, each voter established as the votes route establishes one
```

```text
The verdict moves from Below to In; on a Team with stage rules the same votes leave it In
```

```text
Votes make a Work Item ready and move the verdict only where no stage rule decides readiness
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.API\.Integration\.Refinement\.Slice05NeedAndVerdictTest\.On\_a\_Team\_without\_stages\_a\_vote\_that\_makes\_a\_Work\_Item\_Ready\_moves\_the\_verdict

```text
the port vote step stored the raw browser key instead of the established voter key, which changes whose own vote a reader sees but not readiness
```

###### Obligations

None.

##### Covers

P1

P2

```text
The other fourteen rows of the need-and-verdict class run at the service port with today set explicitly in each Given: the range and its percentiles, blackout days, a Refinement day, the three reasons for no number, the uncut high end, a range of nothing, and a warm read of three hundred Work Items
```

```text
Each row keeps its id and asserts the same facts as on the web host; 17 rows in all pass at the port
```

```text
The Refinement tab gives the same need, verdict and reasons as before, now proven without a web host
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.API\.Integration\.Refinement\.Slice05NeedAndVerdictTest

```text
a fact pinned only by the moved manual-forecast row drifted, which the wire row covers instead
```

###### Obligations

None.

##### Covers

P3

```text
The settings stand-in a port scenario is given, exactly as the port fixture registers it, is asked for a setting no scenario stubbed
```

```text
The read throws, and the failure names the setting read; it never hands back an empty default
```

```text
A port scenario that reads an instance setting nobody gave it fails loud instead of passing on a value no seeded database holds
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Contracts\.Refinement\.RefinementWithoutSignInWireContractTest\.A\_setting\_no\_port\_scenario\_was\_given\_fails\_loud\_naming\_the\_setting

```text
the stand-in were strict for this one setting only and lenient for another unstubbed setting a scenario reads
```

###### Obligations

None.

##### Covers

P3

```text
The port fixture's sign-in answer is compared with what this instance without sign-in answers, and a planted fixture answer saying sign-in is on is put through the same comparison
```

```text
The port answer equals the instance's answer in mode and misconfiguration message; the planted answer is found to differ
```

```text
A port scenario cannot pass on a sign-in mode an instance without sign-in does not have, and the check that says so can fail
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Contracts\.Refinement\.RefinementWithoutSignInWireContractTest\.A\_port\_scenario\_runs\_with\_sign\_in\_as\_an\_instance\_without\_sign\_in\_has\_it

```text
the comparison only looked at a property both answers share by chance; the planted row A_port_fixture_that_says_sign_in_is_on_is_caught_by_the_sign_in_comparison shows it sees the mode
```

###### Obligations

None.

### S3

##### Covers

P1

P2

```text
Run the comments class at the service port: votes with and without a comment, Yes-but conditions, questions from people who did and did not vote, a changed mind, a reader who never voted, comments of 2000 and 2001 characters, empty questions, and a question on a Work Item that is not in refinement, today 7 October 2026
```

```text
Its 22 rows pass under their unchanged names without a web host: the log says who said what, from where and when, oldest first; the open question is marked until its asker votes; each answer lists its current voters in vote order; a refused comment is refused for its reason and leaves the row untouched
```

```text
A maintainer sees the comment and log rules proven at the port, faster, with no test lost
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice12CommentsScenarios\.cs

```text
A comment limit of 1999 or 2001 characters, a question left open after its asker voted, or a log listing the newest entry first; held off by the 2000-character keep-whole rows, the asker-closes row and the oldest-first rows
```

###### Obligations



##### Covers

P2

```text
On the wire, open the log of a Work Item that is not in refinement, the logs of Work Items whose references are REQ/12 and REQ/12#3 after a question was asked on each, and send a question without a name
```

```text
The 4 rows keep their names in the wire class: the log of an unlisted Work Item is 404 while a listed one opens; a slash reference opens, holds the one question asked and dates every entry on the instance's day; a nameless question is 400 with the code voter-name-required and the row stays untouched
```

```text
A browser keeps reaching the log of every Work Item, also those whose tracker reference holds a slash or a hash
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Log\.cs

```text
A log that answers 200 for a Work Item the tab does not list, a slash reference sent unescaped so the route misses it, or an entry without its time; held off by asserting 404 after proving a listed log opens, by reading the entry back for both slash references and by checking each entry's day against the wire host's clock
```

###### Obligations


##### Covers

P2

```text
On the wire, read the log of a Work Item with two votes and a question and read when an entry was recorded
```

```text
Each entry carries exactly kind, answer, comment, voterName, channel, recordedAt, isMine and isOpenQuestion, never a voter key; the log carries only entries and voters; voters has yes, yesBut and no holding names; only the question is flagged open; recordedAt is a UTC time with milliseconds
```

```text
A browser keeps drawing the log, the open question and the time of each entry as it does today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Log\.cs

```text
isOpenQuestion hidden from the JSON, a voter key sent with an entry, or a time without milliseconds; held off by asserting the member names of every entry and the log, the three answer keys holding strings, the flag sequence and the recordedAt text
```

###### Obligations

None.

##### Covers

P2

```text
On the wire, ask a question on a Work Item that is not in refinement, send an empty question, a question of 2001 characters and a vote with a comment of 2001 characters, and ask a valid question
```

```text
The unlisted Work Item answers 409 work-item-not-in-refinement; the empty question 400 comment-required; both too-long comments 400 comment-too-long; a valid question answers 200 with the row of its Work Item flagged as having an open question
```

```text
A browser tells a person why a comment was refused, and shows the question as open as soon as it is sent
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Log\.cs

```text
A refusal with the right status but a wrong or missing code, or an answer row that omits the open-question flag; held off by asserting status and code together on every refusal and the row members on the answer
```

###### Obligations

None.

##### Covers

P1

```text
Run the guard that names every class starting a web host, with the comments class no longer listed as still to move
```

```text
No class outside the still-to-move list or a named wire contract starts a web host, and the comments class is off the list
```

```text
A maintainer is told if the comments class ever starts a web host again
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

```text
The comments class moved but its line left on the list, so a host could return unnoticed; held off by the guard failing on a listed class that no longer starts a host
```

###### Obligations


##### Covers

P4

```text
Measure the three moved classes and the wire class as the earlier timing run did
```

```text
The summed time of the three classes after the move is lower than the 19.06 s before
```

```text
A maintainer waits less for the same three classes
```

##### Disposition

NotTested

```text
A duration is measured by the host's timing run over the trx files, not asserted by a test
```

root\-host\-timing\-run

##### Covers

P2

```text
Run the yardstick class at the port and its wire rows
```

```text
10 rows at the port and 2 in the wire class keep their ids; a fallback window one day wider turns a window-edge row red
```

```text
A maintainer sees no yardstick test lost in the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice10SleYardstickScenarios\.cs

```text
A fallback window one day wider; held off by the first-day and day-before window-edge rows
```

###### Obligations



##### Covers

P2

```text
Run the need-and-verdict class at the port and its wire row
```

```text
17 rows at the port and 1 in the wire class keep their ids; a verdict that uses greater-than where the range includes both ends turns a range-end row red
```

```text
A maintainer sees no need or verdict test lost in the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice05NeedAndVerdictScenarios\.cs

```text
A verdict that treats the high end as outside the range; held off by the in-range rows that sit on both ends
```

###### Obligations



##### Covers

P1

```text
Run the guards on the Team set-up the port writes: a set on a type reachable from a Team, a change to a Team collection, and a guarded table written outside the builder
```

```text
Each planted case turns its guard red, so a moved scenario cannot store what no settings save produces
```

```text
A maintainer trusts that a moved scenario starts from a Team a user can really have
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs

```text
A Team set up by a setter or collection change outside the builder; held off by the planted cases of the guard
```

###### Obligations




##### Covers

P2

```text
On the wire, ask a question of exactly 2000 characters and read the log of its Work Item
```

```text
The question is accepted with 200 and the log holds it whole, so the limit sits at 2000: one more character is refused by the neighbouring rows
```

```text
A person can send a comment of the full length the browser promises
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Log\.cs

```text
A limit of 1999, which would refuse the 2000-character question; held off by asserting 200 and the whole text read back
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A maintainer running the three test classes (SLE yardstick, need and verdict, comments) sees them execute without starting a web host, and the list of tests still waiting to move off a web host no longer names any of them.
```

#### E2

##### Promises

P2

```text
After the move, every test id the three classes had before still exists: 49 rows run at the application-service port and 7 route rows remain in RefinementWithoutSignInWireContractTest, and each earlier assertion is still made somewhere.
```

#### E3

##### Promises

P3

```text
The guard checks named at the earlier slice review (PortGivensGuardTest, E10, E13) each fail when a deliberately planted violating case is introduced, and pass again when it is removed.
```

#### E4

##### Promises

P4

```text
Measured the same way as before, the combined run time of the three classes after the move is lower than the time measured before the move.
```

## Charter

None.

## Examination

None.
