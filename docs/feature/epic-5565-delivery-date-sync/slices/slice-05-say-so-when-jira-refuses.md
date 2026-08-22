# Slice 05 — Say so when Jira refuses the write

**Goal**: an admin whose credential cannot write Releases is told, instead of watching a switch do
nothing.

**Story**: US-06.

## IN scope

- Surface a refused Version write against the work tracking connection, naming the project and the time
  it was last refused.
- Clear the state on a subsequent successful write.
- Do not disable publishing, do not fail the Portfolio refresh, do not retry in a tight loop.
- Inbound keeps working throughout — reading Releases and writing them are separate capabilities (D2),
  which is the Epic's own "an adapter may read without being allowed to write" made real.

## OUT of scope

- A pre-flight permission check. `quiet-jira-writeback` established that `mypermissions` is a reporting
  companion, not a gate in the write path; the same holds here.
- Fixing the permission from inside Lighthouse.
- Email or any other push notification.

## Learning hypothesis

**Disproves that this slice is needed at all** if slice 00's Q3 finds the Version write needs only a
permission most credentials already hold. In that case this shrinks to a log line and the Epic ends at
slice 04 — which is why it is sequenced last.

**Confirms**, if the bar is high, that the failure is legible enough for an admin to act on without
reading server logs.

## Acceptance criteria

AC-06.1 through AC-06.4 in `feature-delta.md`. The one that carries the slice:

- Inbound continues while outbound is refused. A shared "Jira is broken" state that kills date sync
  because a write was refused would be a regression caused by an optional feature.

## Dependencies

Slice 04. Slice 00 Q3 decides whether this slice survives at full size.

## Effort

~3 hours, or ~30 minutes if slice 00 shrinks it to a log line.

## Reference class

`quiet-jira-writeback` slice 05 (`slice-05-writeback-permission-visibility.md`) — same shape: report the
capability gap, do not gate the write on it.
