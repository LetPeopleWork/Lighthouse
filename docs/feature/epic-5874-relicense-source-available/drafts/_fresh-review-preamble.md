# Lighthouse relicensing — fresh review

**LetPeopleWork GmbH · 2026-09-11 · for a reviewer who has seen none of this**

You are the third person to look at this. **That is why this document is
deliberately shaped the way it is.**

Two earlier reviewers have been through several rounds and their findings are
folded into the draft you are about to read. Those findings are **not** in the
main body of this document — they are in Appendix C, at the end, and we would
like you to read Appendix C **only after** you have formed your own view.

The reason is simple: we already know what the first two reviewers think. What we
do not know is what someone reading this cold would notice. If we hand you our
conclusions first, we get them back, and the review is worth very little.

So: two passes, please.

**Pass 1 — cold.** Read sections 1 and 2, then the two documents in Appendix A
and Appendix B. Tell us whatever strikes you: what is unclear, what is
unenforceable, what you would not sign, what a hostile reader would attack, what
a legitimate customer would misread. Please do not look at Appendix C yet.

**Pass 2 — calibrated.** Then read Appendix C, which lists what the earlier
rounds found and what we did about each. Tell us two things:

  (a) **What you found that Appendix C does not contain.** This is the part we
      are actually paying for.
  (b) **What Appendix C contains that you think we got wrong.** Two of those
      items are unresolved disagreements between the earlier reviewers and we
      would value a tiebreak.

---

## 1. What this is, in facts

Lighthouse is a self-hosted delivery-forecasting product from LetPeopleWork GmbH,
a two-person Swiss company. It connects to Jira, Azure DevOps, Linear and
ServiceNow, runs Monte Carlo forecasts on a team's own delivery history, and runs
entirely on the customer's own infrastructure.

It has been on GitHub under the MIT License since 2025. The repository is public:
17 stars, 4 forks (all inert snapshots), Issues and Discussions already disabled.
Eight commits from three outside contributors exist in its history, all accepted
under MIT with no CLA ever in force; about 450 of their lines survive on the
current main branch, of which 59 are in shipped product code.

Revenue comes from a signed licence key that unlocks premium features, sold under
a separate written commercial agreement. There is a free Community edition capped
at three teams and one portfolio. The paid tiers are flat-rate, not per-user.

The last MIT release was **v26.9.9.9, on 9 September 2026**.

## 2. What we are doing, and what is not up for review

We are moving **future** versions to a source-available licence. The attached
`LICENSE` and `NOTICE` are drafts. Nothing has been published; the repository is
still MIT-licensed today.

**The following were decided by the founders before any of this drafting, and are
not what we are asking you to review.** If you think one of them is wrong, say so
— but say so as a separate note, not as a review finding:

1. The source stays public and inspectable on GitHub. This is not a move to
   closed source.
2. OSI-approved status is explicitly not a goal.
3. Every version released up to and including v26.9.9.9 stays MIT in perpetuity.
   The change binds later versions only.
4. The commercial motive is to prevent someone deriving a substitute for
   Lighthouse from its published source — including an organisation building one
   internally rather than paying — and to prevent removal of the licence-key
   gate. AI assistance making derivation cheap is the stated trigger.
5. `LICENSE` reproduces the Elastic License 2.0 verbatim as Part 2 and does not
   modify it. Our own terms sit above it as Part 1, in the same file.

**Everything else is open**, including whether the drafting achieves any of the
above.

## 3. Things we already know we do not know

Stated so you do not spend time telling us:

- None of this has been seen by a lawyer. It goes to counsel next.
- We know the licence binds only people who accept it, and that someone who never
  took the code is outside its reach.
- We know an AI-related clause is unusual and that enforcement would be
  evidentially hard.

---

