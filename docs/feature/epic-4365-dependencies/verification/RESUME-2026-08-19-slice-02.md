# Slice 02 — finished in one sitting, and what is still owed

Written 2026-08-19. Supersedes `RESUME-2026-08-18-slice-02.md`, which described the same slice paused
after phase 01. Every roadmap step is now implemented and green; what remains is a person's work, not a
missing decision.

## What shipped

Thirteen commits on top of `741a94a4f`, one per roadmap step plus a refactor:

| step | what |
| --- | --- |
| 02-01 | the route that lists what a Feature waits on, on both api versions, free and read-only |
| 02-02 | each entry says where it was read from and, if there is one, which of three reasons stands against it |
| 02-03 | a Feature the reader may not see is listed as withheld rather than dropped; nothing in the API writes a dependency |
| 03-01 | the warnings ride on the Feature payload the list already reads, as codes and names |
| 03-02 | a Feature waiting on one placed below it says so, and nothing is moved; an unnumbered read claims nothing about order |
| 03-03 | the loop warning under test end to end, including a hundred-long chain and a Feature that names itself |
| 03-04 | a Feature waiting on one nobody can forecast says so; one with no work left does not |
| 04-01 | the row names what it waits on; the walking skeleton reads it on real Azure DevOps data |
| 04-02 | the warnings column composes the four kinds beside the two it had |
| 04-03 | the terminology scan widened to the two files where the words a reader sees are written |
| 05-01 | at most one decider, only it walks the circles, and neither reaches a repository, a database or a log |
| 05-02 | two operator log events, sized against the noise already there |
| 06-01 | eleven database commands over twenty Features and eleven over two hundred |
| — | refactor: one place builds the sentences, one place answers whether a Feature is readable |

## The dialog was built, reviewed on a running instance, and thrown away

The maintainer looked at it and said no: a number told a reader how many things to worry about and
nothing about which, and the dialog behind it took them out of the list they were reading to answer a
question the row could answer itself. So the column is now called **Dependencies** and lists what a
Feature waits on, one per line, each as `<reference>: <name>` linking into the work tracking system in
a new tab. The dialog, its tests, the client call and the `/features/{id}/dependencies` route that
existed only to feed it are all gone.

One consequence is worth knowing: the entries ride on the Feature payload every list already reads, so
the **Portfolio and Team feature lists name dependencies too** - which the dialog never did. Only the
verdict (what is wrong with a dependency) still needs the whole graph, and the narrower reads say
nothing rather than guessing.

## Owed, and why it was not done

Both need the `:5169` dev instance, which was not running in this session. Starting a dev run against it
creates a second Data Protection key ring that poisons the backend suite and locks `Lighthouse.dll`, and
mutation testing was still to come.

1. **The `:5169` dogfood.** `#5510` should list `#5511`; `#5511` should list `#5512` and `#5733`, with
   `#5733` (no child Work Items) producing the cannot-be-forecast warning on `#5511`'s row; `#5512` and
   `#5733` must still read empty. Also the ≤200 ms wall-clock figure the slice brief records as owed.
2. **Screenshots** of the dialog and of each warning kind, same day, against real data.

Also unanswered by anything but a person: whether a second Portfolio is worth creating on `:5169` so
cross-Portfolio runs on real data rather than on fixtures. The loop cannot be dogfooded on Azure DevOps
at all — that is settled, and moves to slice 03 with Jira.

## Two things the next session should not re-derive

- **A brand-new local instance fails the first E2E run and passes the second.** Seen twice: the very
  first run after starting the app times out waiting for the Portfolio link on the overview, and an
  immediate re-run passes in about three seconds. It is a cold-start artefact of the instance, not the
  spec.
- **Dependency warnings are absent, not empty, on `/features/ids` and `/features/references`.** Whether
  a dependency can be acted on is a question about the whole graph, so a request for a handful of
  Features cannot answer it honestly - it would report Features as unreachable merely because nobody
  asked for them. The consequence is real and worth a decision: the Portfolio and Team feature lists
  show the Depends On count but no dependency warnings.
