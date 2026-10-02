# Slice 19 — Spike: remote facilitated session

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E4 Live Refinement sessions (new) · **Type**: Spike ·
**Timebox**: ≤1d · **Tier**: Community

## Learning objective

Can participants join a live, facilitated Refinement from their own devices, and what identity does each one carry?

1. The only SignalR hub is `[Authorize]` and carries refresh status only (X4). Does the shared auth-off subject pass
   `[Authorize]`? If it does, how does a per-browser self-declared name travel with each message (DD-10)?
2. With auth on, every participant has an account (D14). Is the existing hub, with a new group per Team, enough?
3. Hosted and multi-replica: does the Redis backplane (`Program.cs:333`) need anything new?
4. Does this widen the attack surface the way the accepted embed-nonce risk did (R5)? What must DESIGN rule out?

## Output

A short note to feed E4's next DISCUSS and an ADR draft in DESIGN. It names a go or no-go, and the identity model for
each auth mode. **No production code.**

## OUT

Building the remote mode.

## Dependencies

Slice 18 shipped. Learn what the room needs before going remote.
