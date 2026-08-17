# Slice 08 — The key that won at startup is the key that stays

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5795 · **Estimate**: ~2h
**Origin**: not from DISCUSS. Found by the independent security review of 2026-08-17
(`verification/security-review-findings.md`, finding E2; prompt in `security-review-brief.md`).
**Ordering**: after slice 07, before slice 09. It blocks the release. Cheapest of the three by a
distance, and the only one whose defect can strand credentials without anybody doing anything wrong
after the misconfiguration.

## Goal

The ordered list in `EncryptionKeyRingBootstrapper.Resolve` is the only thing that decides which key an
instance runs on — at the moment it starts, and at every moment after.

## The defect

`Program.WatchTheMountedKeysFile` registers the reload watcher whenever `Encryption:KeysFile` holds a
value. It never asks whether the mounted file actually *answered* the resolution. `KeyRingFileWatcher.Apply`
then replaces the ring with whatever the file parses to, comparing it only against the ring in force —
it consults neither custody nor precedence, because it was written for the deployment where the file is
the only source there is.

So an instance with both `Encryption__Key` and `Encryption__KeysFile` set boots on the configuration key,
reports custody *supplied by configuration*, and moves to the file's key within thirty seconds. Every
secret written under the configuration key in that window becomes unreadable, because the file's ring
does not carry it. On the next restart configuration wins again and everything written under the file's
key becomes unreadable in turn. An instance can sit in that loop indefinitely, losing a little on each
side of every restart.

This is the exact failure the ordering was written to prevent. The comment above `Resolve` says it
plainly — "an instance that made its own key while an operator was also supplying one would lose that
argument again on the very next start, and by then every secret written under the key it made would be
unreadable" — and then a second, shorter list contradicts it thirty seconds later.

The panel makes it worse rather than better. `EncryptionController.WhatTheKeyArrivedIn` asks
configuration first, so with both set it names `Encryption__Key` as the setting that answered while the
file's key is the one in force. An administrator debugging unreadable credentials is sent to edit the
setting that is not being used.

The chart cannot reach this on its own: it sets only `Encryption__KeysFile` and exposes no extra-env
passthrough. The reachable population is Compose and bare installs, and anyone moving from an
environment-variable key to a file who leaves the old variable behind — which is the migration the
configuration page encourages.

## IN scope

- **The watcher is registered only where the mounted file is the source that answered** — that is, where
  the resolved custody is `SuppliedByExternalSecret`. Registration already happens inside
  `EnsureEncryptionKeyRing`, which holds the resolved ring, so the fact is on hand and does not have to
  be re-derived.
- **A start that supplies a key two ways says so.** Not a refusal — an instance that boots correctly
  today must keep booting — but a line naming both settings and which one is in force, so the operator
  who set the second one finds out from Lighthouse rather than from a credential that stopped working.

## OUT of scope

- Refusing to start when both are set. It matches the shape the chart already uses for the same question
  and it is the tidier rule, but it would stop instances that boot fine today for a misconfiguration
  that, once the watcher is fixed, costs them nothing. If the warning turns out to be ignored, refusing
  is the next move and it is a one-line change from here.
- Anything about what the panel shows. `WhatTheKeyArrivedIn` becomes correct as a consequence of this
  fix, because custody and the setting that answered can no longer disagree; it needs no edit of its own.
- The re-encryption pass, whose exposure to a mid-flight ring change is slice 09.

## Learning hypothesis

**Disproves** "registration is the only place precedence is re-decided" if reading the wiring turns up
another consumer of `Encryption:KeysFile` that resolves independently of the bootstrapper. There is one
today — the watcher constructs its own `MountedFileKeyRingSource` from the raw configuration value
rather than being handed the one the bootstrapper used — and whether that second construction should
survive this slice is the question worth answering while the file is open.

**Confirms**, if it holds, that the ordering is genuinely written down once and that the defect was a
registration condition rather than a design gap.

## Acceptance criteria

- **AC-8.1** — With both `Encryption__Key` and `Encryption__KeysFile` set and naming different keys, the
  instance boots on the configuration key and is still on it after several reload intervals have passed.
  Driven through the real configuration provider, because the binding behaviour is the thing under test.
- **AC-8.2** — With only `Encryption__KeysFile` set, a key added to the file is picked up exactly as it
  is today. The reload is not weakened for the deployment it exists for.
- **AC-8.3** — An instance that resolved its key from configuration while a keys file is also present
  says so once at startup, naming both settings and which answered.
- **AC-8.4** — The encryption panel names the setting the key actually arrived in, on an instance with
  both set. This holds without editing `WhatTheKeyArrivedIn`; if it does not, the fix is incomplete.

## Dependencies

Slices 01–06b, and slice 07 ahead of it only for release sequencing — there is no technical dependency
between them. No new data, no migration. One new startup line, no new configuration name.

## Dogfood moment

Same day: a local instance with both set, a credential saved, then wait out two reload intervals and
save a second credential. Both must still read after a restart. Today the second one does not.

## Pre-slice SPIKE

None.

## Verdict

_To be recorded at slice close: confirmed / disproved._
