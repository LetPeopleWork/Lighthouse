# Slice 02 — Nothing waits behind itself

**Story** #6055 / US-02 · **Job** `job-operator-see-what-lighthouse-is-doing-right-now`

## Goal

The one field in the Activity list whose whole purpose is to explain a wait stops naming the waiting
row's own entity as the thing it is waiting for.

## IN scope

- **The backend decides whether the holder is this row's own entity** (D3), on the read path, where it
  already picks the holder (`UpdateController.cs:61-62`) and already resolves its name. No extra query.
- **The response gains the minimum that lets the browser word it** — the field is DESIGN's call, and
  `53aa75a1b` is prior art arguing it may need nothing at all. Read that commit before writing code.
- **The browser words the clause** (D3), because the words are terminology-dependent. Where the holder
  is a different entity the clause is unchanged: `Queued behind Ocean Explorer`.
- **Where the holder is the same entity, the clause names the holder's activity** (D4):
  `Queued behind its own refresh`, `… its own forecast`, `… its own removal`.
- **A contract guard.** Every other field of `UpdateTaskResponse` is asserted byte-identical against the
  serialised payload, because this is a shared contract with one producer and one consumer and the
  guard belongs where a consumer would break.

## OUT of scope

- **Anything about the queue.** One lane in, one lane out. #5877's per-type lanes shipped on 2026-09-19
  and were reverted the same day for destroying Feature ownership under concurrent refreshes; nothing
  here re-lands them, reintroduces `UpdateLane`/`UpdateLaneMapping`, or depends on lanes existing (D5).
- The row's verb-and-kind phrasing — slice 01.
- The `Stopping…` and terminal states, which carry no clause.
- Queue position or an estimated wait, refused in #5511 (D15) and again in #5877.

## Learning hypothesis

**Disproves that "the same entity" is cheaply knowable on the read path.**

The whole slice rests on one assumption: that `UpdateTaskType` already distinguishes entity kinds, so
comparing (kind, id) needs no new lookup and no extra load. If it turns out that deciding "same entity"
needs anything the read path does not already hold — a repository round-trip per queued row, say — then
the clause costs a query per row on a popover that re-reads on every status change, and the honest
alternative is to drop the clause in the same-entity case rather than to explain it.

AC-02.4 is the assumption stated as a test: a `Team` with id 3 queued while `Features` for portfolio 3
runs is **not** a self-reference. An implementation that compares ids alone passes every other AC in
this slice and fails that one.

If it succeeds, both halves of #6055 are closed, and a row that is genuinely waiting says so in a way
that stays true whether the queue has one lane or several.

## Acceptance criteria

AC-02.1 through AC-02.8 in `feature-delta.md` — 8 ACs. The four carrying the risk:

- **AC-02.4** — the hypothesis as a test. Same id, different entity kind, no self-reference.
- **AC-02.6** — every field that exists today keeps its name, type and meaning, asserted on the
  serialised payload. The guard that makes touching a shared contract a safe move rather than a hopeful
  one.
- **AC-02.8** — one decider. If a comparison of a row against the lane holder ends up in the browser
  too, the two will answer differently under a partial re-read, which is the failure
  `ActivitySection.tsx:48-55` already warns about in its own comment.
- **AC-02.7** — **production data.** A real Portfolio refresh on the dogfood instance and its triggered
  forecast, with the queued row not naming its own Portfolio.

## Dependencies

- **Slice 01, by reading order rather than mechanically.** The clause is worded in the vocabulary slice
  01 establishes. It would compile first; it would read as a fix bolted to an unfixed row.
- **DESIGN's answer on what the response gains, if anything.** Cannot start without it; it is the first
  item in the handoff, and `53aa75a1b` is the evidence it turns on.
- **#5877's state at the time.** Not a blocker — D4's rule holds under one lane and under many — but
  both change the same `waitingBehind` resolution in `UpdateController`, so whichever lands second
  rebases. Check before starting.

## Effort estimate

**~3h of crafter dispatch.** One record widened, one interface line, one clause, and the tests that
hold the contract still.

## Reference class

`epic-5511-task-manager` slice 07 — the slice that introduced `waitingBehind` in the first place. Same
producer, same consumer, same acceptance fixtures.

Closer still: `53aa75a1b`, #5877's own reverted fix to this field. Four files, 100 insertions, no
contract change — a realistic upper bound on this slice, and the shape to argue with rather than the
shape to copy.

## Pre-slice SPIKE

**None.** The uncertainty is whether (kind, id) is enough to decide sameness, and that is AC-02.4 — an
acceptance criterion against code already in production, not something a probe would learn first.
