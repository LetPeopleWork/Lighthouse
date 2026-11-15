# Slice S3 - votes and comments of the refinement view: moved assertions

The scripts `measure-slice.sh`, `check-slice.sh` and `deliberate-breaks.sh` read this file by slice id. Ids are
`file :: full test name`, files relative to `Lighthouse.Frontend`.

## Measure

- Before commit: 6bdd76fa5056704c11702c176bbbf85e52000f73
- Page test file: src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx
- Direct test files: src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts
- File-alone ratio at most: 0.60
- Combined ratio at most: 0.80
- Production allow-list: none

One row per test of the page file at the before commit. A kept test keeps its id; a removed or merged test names the
tests that now prove its rule, separated by `;;`. The direct test files list every test they hold under "Added ids",
the ones that already existed included, because the check compares the whole run of those files.

## Moved assertions

| Old id | Disposition | Rule proved | New tests |
|---|---|---|---|
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab asks for the condition of a Yes, if… and sends it, trimmed, with the vote in one request | removed | the condition prompt names the Work Item, asks what has to be true, takes 2000 characters, confirms a padded condition trimmed; the vote waits for it and is cast once with it as the comment | src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx :: asking for the condition of a Yes, if… names the Work Item and asks what has to be true, with room for 2000 characters ;; src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx :: asking for the condition of a Yes, if… confirms a padded pasted condition once, trimmed ;; src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a 'Yes, if…' from the tab asks for its condition and casts nothing until it is given, then casts once with it as the comment ;; src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab asks a browser without a name who is voting first, then the condition of its Yes, if… |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab will not cast a Yes, if… whose condition is blank | removed | Vote stays disabled for a blank or spaces-only condition and confirms nothing | src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx :: asking for the condition of a Yes, if… keeps Vote disabled for a blank condition and confirms nothing |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab casts nothing when the condition of a Yes, if… is cancelled | removed | Cancel on the prompt calls only onCancel, and a cancelled condition casts nothing | src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx :: asking for the condition of a Yes, if… calls only onCancel when it is cancelled, even with a condition typed ;; src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a 'Yes, if…' from the tab casts nothing when its condition is cancelled |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab asks a browser without a name who is voting first, then the condition of its Yes, if… | kept | a browser without a name is asked who votes, then the condition; one castVote carries answer, channel, name, trimmed condition and key |  |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab lets anybody add a comment without voting | kept | a comment from the votes and comments is answered by the Warnings cell, addComment gets comment, channel, name and key, the log is read again so the comment shows, and the first log read carried the browser key |  |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab reads the log again once a comment is sent, so the comment shows in it | removed | the log is read again after a comment so the comment shows in it (merged into the kept comment test) | src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab lets anybody add a comment without voting |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab shows the log of the Work Item now open when a comment sent on another one is answered late | kept | a comment answered late leaves the log of the Work Item now open |  |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab reads the log once more, not twice, when somebody names themselves to comment | removed | a key that arrives while the log is open does not read again and the next read carries it; a browser without a name is asked and the comment sent under it once | src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: which Work Item's log is read again, and for whom does not read again for a key that arrives while the log is open, and sends it with the next read ;; src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx :: a comment the server refuses, and a comment from a browser without a name asks a browser without a name for it and sends the comment under it once |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab sends a comment once, however often Send is pressed while it is on its way | removed | a second comment on a Work Item waits for the first answer, and Send and the box are disabled while it is on its way | src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx :: adding a comment to a Work Item without voting sends a second comment on a Work Item only once the first has been answered ;; src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: sending a comment from the votes and comments of a Work Item disables Send and the comment box while a comment is on its way |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab keeps a refused comment so it can be sent again | kept | a refused comment shows its error, keeps the text and Send is enabled again |  |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab takes a comment of at most 2000 characters | removed | the comment box takes 2000 characters | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: sending a comment from the votes and comments of a Work Item takes at most 2000 characters and keeps Send disabled for a blank comment |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab will not send an empty comment | removed | Send is disabled for a blank comment and sends nothing; a refused comment reaches onFailure, never onSent, and is no longer counted as being sent | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: sending a comment from the votes and comments of a Work Item takes at most 2000 characters and keeps Send disabled for a blank comment ;; src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx :: a comment the server refuses, and a comment from a browser without a name reports a refused comment, never says it was sent, and stops counting it as being sent |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab marks a Work Item's comments in its Votes cell and warns of its open question in its Warnings cell, leaving the others unmarked | kept | Votes cell and Warnings cell marks, the others unmarked |  |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab lists what people wrote oldest first, naming who and on which day, and leaves out votes without words | removed | only entries with words are listed in log order with who, day and condition; the open read carries the browser key | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list lists only what people wrote, in the order of the log, naming who, on which day, and the condition ;; src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: which Work Item's log is read again, and for whom reads the open Work Item's log with the key of the browser that opened it |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab names the people whose current vote a count in the split is, on hovering or focusing it | removed | a count names its voters on hover and focus, none for a count of 0, counts from the log voters | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list names the people whose current vote a count in the split is, on hovering or focusing it, and none for a count of 0 |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab reads the log without the browser's key for somebody signed in | kept | the log is read without the key for somebody signed in |  |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab marks as open questions the entries the server calls open, even between two people sharing a name | removed | open question marked only where the entry says so, also between people sharing a name | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list marks as an open question only the entries the log calls open, also between two people sharing a name |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab shows a comment as the text it is, never as markup | removed | markup in a comment is shown as text | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list shows a comment as the text it is, never as markup |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab never says where an entry came from | removed | no channel wording in the list | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list never says where an entry came from |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab adds nothing to what people wrote when a vote is taken back | removed | a revocation adds nothing to the list | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list adds nothing when a vote is taken back, even one that still carries its words |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab lets a reader who never voted read how the votes split, who cast them, and every comment | removed | a reader who never voted sees the split from the log voters, who cast them, and every comment | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list shows a reader who never voted how the votes split, who cast them, and every comment |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab says so when nobody has written anything about a Work Item, even when it has votes | removed | No comments yet when only votes exist | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list says so when nobody has written anything, even when the Work Item has votes |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab says why when a Work Item's log cannot be read because the Work Item left refinement | kept | one log-cannot-be-read row stays on the page: the renamed terms, 404 |  |
| src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx :: Comments, conditions and questions on the Refinement tab says why when a Work Item's log cannot be read because the log could not be read | removed | a failed read shows the alert with the describeFailure text; any other error returns its message | src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list shows why the log could not be read in the words it is given ;; src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: why a Work Item's log could not be read returns the message of a refusal other than not found |

## Added ids

- src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx :: asking for the condition of a Yes, if… calls only onCancel when it is cancelled, even with a condition typed
- src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx :: asking for the condition of a Yes, if… confirms a padded pasted condition once, trimmed
- src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx :: asking for the condition of a Yes, if… keeps Vote disabled for a blank condition and confirms nothing
- src/pages/Teams/Detail/Refinement/ConditionPrompt.test.tsx :: asking for the condition of a Yes, if… names the Work Item and asks what has to be true, with room for 2000 characters
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: sending a comment from the votes and comments of a Work Item disables Send and the comment box while a comment is on its way
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: sending a comment from the votes and comments of a Work Item sends the trimmed comment, keeps the draft until the server has taken it and clears it then
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: sending a comment from the votes and comments of a Work Item takes at most 2000 characters and keeps Send disabled for a blank comment
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: the votes and comments of one Work Item counts the votes from the same reading that names the voters, so the count and the names agree
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: the votes and comments of one Work Item names nobody behind a count while the voters are still being read
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: the votes and comments of one Work Item offers no name to change to a reader without one
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: the votes and comments of one Work Item reads a Work Item the server sent no split for as nobody having voted
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: the votes and comments of one Work Item reads a Work Item the server sent no split for as nobody having voted while the voters are still being read
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: the votes and comments of one Work Item shows the Work Item's own count while the voters are still being read
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list adds nothing when a vote is taken back, even one that still carries its words
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list lists only what people wrote, in the order of the log, naming who, on which day, and the condition
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list marks as an open question only the entries the log calls open, also between two people sharing a name
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list names the people whose current vote a count in the split is, on hovering or focusing it, and none for a count of 0
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list never says where an entry came from
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list says so when nobody has written anything, even when the Work Item has votes
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list shows a comment as the text it is, never as markup
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list shows a reader who never voted how the votes split, who cast them, and every comment
- src/pages/Teams/Detail/Refinement/VotesAndCommentsDialog.test.tsx :: what the votes and comments of a Work Item list shows why the log could not be read in the words it is given
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it counts a comment as written: { kind: 'Comment', answer: null, …(6) }
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it counts a vote taken back as written: { kind: 'Revocation', …(7) }
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it counts a vote taken back that still carries the words it had as written: { kind: 'Revocation', …(7) }
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it counts a vote with words as written: { kind: 'Vote', answer: 'Yes', …(6) }
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it counts a vote without words as written: { kind: 'Vote', answer: 'Yes', …(6) }
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it heads a Comment answering null as 'Ana Lima'
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it heads a Vote answering No as 'Ana Lima · No'
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it heads a Vote answering Yes as 'Ana Lima · Yes'
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it heads a Vote answering YesBut as 'Ana Lima · Yes, if…'
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it heads an entry that came through Assistant like any other
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it heads an entry that came through Cli like any other
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it heads an entry that came through Web like any other
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: what somebody wrote, as the votes and comments list it names the day an entry was recorded
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: why a Work Item's log could not be read returns the message of a failure that is not a refusal
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: why a Work Item's log could not be read returns the message of a refusal other than not found
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: why a Work Item's log could not be read returns the message of something that is not an error at all
- src/pages/Teams/Detail/Refinement/sizingLogWording.test.ts :: why a Work Item's log could not be read says, in the renamed terms, that the Work Item left refinement when the server finds no log for it
- src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx :: a comment the server refuses, and a comment from a browser without a name asks a browser without a name for it and sends the comment under it once
- src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx :: a comment the server refuses, and a comment from a browser without a name reports a refused comment, never says it was sent, and stops counting it as being sent
- src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx :: adding a comment to a Work Item without voting comments on the Team the tab shows now, not the one it opened on
- src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx :: adding a comment to a Work Item without voting never counts a commented Work Item as just made ready, whatever the server's row says
- src/pages/Teams/Detail/Refinement/useCommentAdding.test.tsx :: adding a comment to a Work Item without voting sends a second comment on a Work Item only once the first has been answered
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: the log of the Work Item whose votes and comments are open drops a second read of the Work Item open before, once another one is open
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: the log of the Work Item whose votes and comments are open drops an answer for the Work Item open before, even while the new one is still being read
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: the log of the Work Item whose votes and comments are open drops an answer that arrives after the log was closed
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: the log of the Work Item whose votes and comments are open keeps the open Work Item's log when the one open before fails after it
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: the log of the Work Item whose votes and comments are open reads again on request from the Team the tab shows now
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: the log of the Work Item whose votes and comments are open reads the open Work Item's log again for the Team the tab moved to
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: the log of the Work Item whose votes and comments are open shows nothing of the Work Item open before while the next one is read
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: which Work Item's log is read again, and for whom does not read again for a key that arrives while the log is open, and sends it with the next read
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: which Work Item's log is read again, and for whom reads nothing for a Work Item that is no longer the one open
- src/pages/Teams/Detail/Refinement/useSizingLog.test.tsx :: which Work Item's log is read again, and for whom reads the open Work Item's log with the key of the browser that opened it
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a 'Yes, if…' from the tab asks a browser without a name for the name first and for the condition second
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a 'Yes, if…' from the tab asks for its condition and casts nothing until it is given, then casts once with it as the comment
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a 'Yes, if…' from the tab casts nothing when its condition is cancelled
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a vote from the tab casts a 'Yes, if…' that already carries its condition without asking for one
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a vote from the tab casts for the Team the tab shows now, not the one it opened on
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a vote from the tab casts nothing when a name is given with no vote waiting for it
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a vote from the tab drops a 'Yes, if…' still waiting for its condition when the Team changes
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a vote from the tab drops a vote still waiting for its voter's name when the Team changes
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: casting a vote from the tab sends a second vote on a row only once the first has been answered
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: taking one's own vote back from the tab never counts the row it answers as just made Ready
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: taking one's own vote back from the tab takes it back on the Team the tab shows now, not the one it opened on
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: when a vote counts as cast, relative to the Team's Refinement is NoCadence before the tab has loaded
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: when a vote counts as cast, relative to the Team's Refinement with the next Refinement on 2026-10-08 and a Refinement day false is OnOtherDay
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: when a vote counts as cast, relative to the Team's Refinement with the next Refinement on 2026-10-08 and a Refinement day true is OnRefinementDay
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: when a vote counts as cast, relative to the Team's Refinement with the next Refinement on 2026-10-08 and a Refinement day undefined is OnOtherDay
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: when a vote counts as cast, relative to the Team's Refinement with the next Refinement on null and a Refinement day false is NoCadence
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: when a vote counts as cast, relative to the Team's Refinement with the next Refinement on null and a Refinement day true is NoCadence
- src/pages/Teams/Detail/Refinement/useVoteCasting.test.tsx :: when a vote counts as cast, relative to the Team's Refinement with the next Refinement on undefined and a Refinement day undefined is NoCadence

## Assertions the kept page tests must carry

The page file after the rewrite holds exactly the seven kept tests, each under the same full name, in the same file
order. Two of them absorb assertions of removed tests; the crafter adds exactly these, and the completion review reads
them in the file because the scripts check names only. Every kept test keeps every assertion it has today.

- "asks a browser without a name who is voting first, then the condition of its Yes, if…": the condition is pasted
  padded (`  ${CONDITION}  `, as the removed first test did) and the single castVote still carries the trimmed
  CONDITION, answer, channel, name and the stored key; after Vote the confirmation dialog closes (no dialog left).
- "lets anybody add a comment without voting": the log service answers its first read with an empty log and every later
  read with the question from the commenter (`mockResolvedValueOnce(aLog([])).mockResolvedValue(aQuestionFrom(JONAS))`);
  after Send, besides the Warnings cell and the addComment arguments it already asserts, the open votes and comments
  list the question (visible, exactly one list item), the first getLog call carries the browser key and a later one
  carries it too. The removed "reads the log again" test is merged here, so this test owns that wiring.
- "shows the log of the Work Item now open when a comment sent on another one is answered late": unchanged; with the
  open-reference guard removed from the hook, the read of the first Work Item replaces the open log and this test goes
  red (a second proof next to the hook test).
- "says why when a Work Item's log cannot be read because the Work Item left refinement": the `it.each` of two rows
  becomes one plain test with this exact title; the 500 row goes to the dialog and the wording function.
- The four other kept tests stay as they are.

## Deliberate breaks that name a kept page test

One break line names a kept page test: the open-reference guard of useSizingLog with the late-answer page test. It goes
red today, because that test already sets a different question per Work Item (Ana for the first, Mo for the second),
and it must stay red after the rewrite. Every other break names a direct test and goes red today on the direct file
this slice authors. If the page rewrite drops an assertion listed above, no break notices it: the completion review
reads the file for those assertions.
