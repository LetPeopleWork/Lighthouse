# Slice 17b — Cast (and take back) a sizing vote from the CLI or an assistant

**Feature**: epic-5510-5881-refinement · **Epic**: E3 Sizing votes (#5510) · **Story**: US-17b ·
**Estimate**: ~1d · **Tier**: Community · **Repo**: `lighthouse-clients` (plus a small amount of Lighthouse work if the
endpoint needs the "cast from a client" marker)

## Goal

The CLI and MCP can cast Yes, "Yes, but…" with its condition, or No, each with an optional comment. They can also
leave a comment-only question (DD-11). Once slice 16 exists, they can take a vote back. The vote goes into the same
log, with the same permission (Team read) and the same identity rules as the UI (DD-19).

## IN

- A CLI vote command and an MCP vote tool. Working names: `teams refinement vote <team> <workItem>` and
  `vote_on_work_item`.
- Identity:
  - auth on: the person behind the client's credential. If the credential belongs to no person, the vote is
    refused.
  - auth off: a self-declared name the client supplies. It is required and never defaulted.
- The hidden split (DD-12) applies to the client caller.
- "Take back" is wired only if slice 16 has shipped. Otherwise it is left out of this slice, not stubbed.
- A Lighthouse-Clients **minor** bump with a changeset. This is a write, so it gets a minor bump, not a patch.

## DESIGN must decide (DD-19 a–d), before this slice starts

a) Whether a client credential (API key or token) resolves to a person. If it can belong to an instance or a
   service, votes under it must be refused, not attributed.
b) Where the auth-off name lives on the client: a flag, a config file entry or an MCP tool argument.
c) Whether the log marks a vote as cast from a client. This is recommended, and it is also a candidate closed-enum
   property for DEVOPS.
d) The wording of the MCP tool description, so that an assistant confirms with the user before casting. The vote is
   the user's judgement, and D30 depends on it.

## OUT

Bulk voting. Voting on behalf of someone else. Changing readiness settings.

## Learning hypothesis

**This disproves "a vote cast from a terminal or assistant is still a person's judgement"** if the named voter does
not recognise a client-cast vote during dogfood. If that happens, (d) is not enough, and client votes need an
explicit confirmation step or a separate marker in the UI.

## Data and dogfood moment

- Demo instance (auth off): cast Ana Lima's "Yes, but…" on GR-051 from the CLI.
- Premium dev instance (auth on): vote with a personal credential, and check that a non-personal credential is
  refused.
- Dogfood: for one Refinement cycle, a voter casts some votes through the MCP assistant. Afterwards, ask whether every
  one of them was intended.

## Acceptance criteria

AC-17b.1 … AC-17b.5 (US-17b).

## Dependencies

Slices 11 (votes), 12 (comments and conditions) and 17a (shared response shape). Slice 16 is needed for take-back
only.
