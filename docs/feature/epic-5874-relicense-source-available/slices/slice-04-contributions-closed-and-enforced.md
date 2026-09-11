# Slice 04 — Contributions closed, and enforced

**Feature**: `epic-5874-relicense-source-available` | **Stories**: US-04 | **ADO**: [#5971](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5971) | **Estimate**: ~4h
**Depends on**: nothing. This slice ships independently of the other three.

## Goal

Someone who opens a code pull request against Lighthouse is told within a minute that code
contributions are not accepted, and where to send their report or idea instead — so the position is
enforced by the repository rather than by a maintainer answering each one by hand.

## Learning hypothesis

**`pull_request_target` is the only event that can close a fork PR, and it is the one with the
sharp edge.**

Disproves if it fails: the assumption that "add a workflow that closes PRs" is a five-minute job. A
plain `pull_request` workflow triggered from a fork runs with a read-only token and cannot close
anything. `pull_request_target` runs in the base repo's context with a writable token — which is why
it works, and why GitHub warns loudly against checking out the PR's head in it. This workflow must
never check out the contributor's code; it reads
`github.event.pull_request.head.repo.full_name`, compares, comments, closes, and does nothing else.
If the first attempt uses `pull_request` and silently no-ops, that is the confirmation, and it is
much better found here than discovered as a stream of open PRs nobody was notified about.

Confirms if it succeeds: that AC-04.3 and AC-04.4 can both be satisfied by one small workflow, and
that maintainer PRs from in-repo branches keep working untouched.

## Production data

No data path. The acceptance bar is a **real pull request**: one opened from an actual fork (closed
automatically), and one opened from an in-repo branch (left alone). Both must be observed, not
reasoned about — AC-04.4 is the one that breaks the maintainers' own workflow if it is wrong.

## Dogfood moment

Same day: open a throwaway PR from a personal fork, watch it close, and read the comment as an
outside contributor would. Then open a throwaway PR from a branch in the repo and confirm nothing
happens to it.

## IN scope

- `docs/contributions/contributions.md` rewritten (AC-04.1, AC-04.2). Its opening line today is "We
  develop Ligththouse as an Open Source project, so that people can actively contribute" — that is
  the sentence the new licence contradicts, and it also carries a typo that has been live for a
  while. What stays welcome is named explicitly: bug reports, feedback, docs corrections, word of
  mouth. Existing contributors keep their thanks, with no implication the door is still open.
- A line in the same page recording that forks cannot be disabled on a public repository and that
  this is accepted (AC-04.5) — so the question is answered once instead of re-asked every time
  someone notices the fork count.
- `.github/PULL_REQUEST_TEMPLATE.md` stating the same thing, so a contributor sees it before they
  press the button rather than after.
- A `pull_request_target` workflow that closes a PR whose head repo differs from the base repo and
  comments with the feedback channels (AC-04.3), and that leaves in-repo PRs alone (AC-04.4). It
  checks out nothing.

## OUT of scope

- Disabling forks — **not possible** on a public repository, and not treated as a problem. The four
  existing forks are inert snapshots under a perpetual MIT grant.
- Disabling pull requests — GitHub offers no such setting; the workflow is the mechanism.
- Interaction limits. They expire after at most six months and would need re-arming forever.
- `CONTRIBUTORS.md` at the repo root. It is already only a pointer to the docs page, and the pointer
  stays correct — AC-04.2 asks that this be verified, not rewritten.
- Any other docs page. Slice 02 owns those and deliberately leaves `contributions.md` to this slice.

## Acceptance criteria

AC-04.1 through AC-04.5 as stated in `feature-delta.md`.

## Note for the crafter

The canonical contributors list is the docs page, not the root `CONTRIBUTORS.md`. Edit
`docs/contributions/contributions.md`; the root file is a pointer and stays one.
