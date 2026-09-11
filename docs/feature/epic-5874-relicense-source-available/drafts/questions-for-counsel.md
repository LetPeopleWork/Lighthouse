# Lighthouse relicensing — questions for counsel

**Prepared 2026-09-11.** Send with three documents: `LICENSE.draft`,
`LICENSE-ADDITIONAL-TERMS.md`, `NOTICE`.

## What we are doing, in four sentences

Lighthouse is a self-hosted delivery-forecasting product from LetPeopleWork GmbH,
published on GitHub under the MIT License since 2025. We are moving future
versions to a source-available licence: the code stays public and inspectable,
customers may still read, run and modify it, but competing products are
restricted. We are **not** going closed source, and we are **not** seeking
OSI-approved status. Everything released up to and including v26.9.9.9
(2026-09-09) stays MIT permanently; the change binds later versions only.

The instrument is two documents read together: `LICENSE` reproduces the Elastic
License 2.0 **verbatim and unaltered** beneath a header block of ours, and
`LICENSE-ADDITIONAL-TERMS.md` adds two clauses. We chose that shape deliberately:
Elastic publishes ELv2 for others to adopt, but grants nothing about publishing a
*modified* version of its text, so we do not modify it.

---

## A. Structure — the questions that decide whether this works at all

**A1. Does a second document, incorporated by reference, bind someone who only
ever opens `LICENSE`?**
This is our central structural question. Our mitigation is a header block at the
top of `LICENSE`, above Elastic's text, naming the composite and pointing at the
second file. Is that sufficient, or do the additional terms need to be physically
inside the same file to bind reliably?

**A2. Is the grant correctly framed?**
ELv2 grants a licence to "use, copy, distribute, make available, and prepare
derivative works". Our header says that grant is offered *subject to* the
Additional Terms, and that the Additional Terms govern on conflict. Is it
stronger to frame this as a **narrower grant from the outset** (the right was
never given) rather than as a **restriction on a right already granted**? We
would rather not rely on a conflict-resolution sentence if a narrower grant is
cleaner.

**A3. Reproducing Elastic's text under our own name — any exposure?**
We reproduce ELv2 unaltered, state plainly that the composite is not ELv2 and
must not be identified as `Elastic-2.0`, and that we are not affiliated with or
endorsed by Elasticsearch B.V. Is reproducing a third party's licence text
inside our own licence, under a different name, a copyright or trademark
concern? Is our disclaimer adequate, or is something else required?

**A4. Do we need to name a governing law and jurisdiction?**
There is currently **no governing-law or jurisdiction clause anywhere** — ELv2
has none, and our Additional Terms add none. We are a Swiss GmbH selling
internationally, mostly into Europe. Should we add one, and if so, what?

**A5. What happens when someone breaches our Additional Terms?**
ELv2's Termination clause is triggered by using the software "in violation of
these terms", where "these terms" reads as ELv2's own. Breaching section 1 or
section 2 of our Additional Terms may therefore have **no stated consequence**.
Should the Additional Terms carry their own termination language, or expressly
adopt ELv2's, including its 30-day cure period?

---

## B. Scope — is section 1 drafted so people can actually apply it?

**B1. Is "a separate product that substitutes for it" determinate enough?**
Section 1 draws its line between a customer's own deployment of Lighthouse
(always permitted, however heavily modified) and a separate product built out of
it (not permitted, whether shipped externally or only deployed internally). A
first draft blurred this and a reviewer caught the contradiction. Is the revised
wording clear enough to be enforceable, or is it void for uncertainty? If the
latter, how would you draw the same line?

**B2. Does the internal-deployment restriction survive?**
We deliberately cover the case of an organisation building its own substitute
from our source and deploying it only to its own staff, never selling it. That is
the commercial risk we care most about — AI assistance has made it cheap. Is a
restriction on purely internal use enforceable, and does it create any
unfair-terms exposure in standard business terms (we are aware this is a live
question under Swiss and German law)?

**B3. Is the cross-language and cross-platform reach overbroad?**
Section 1 says products compete even in a different interface, platform or
programming language. A reviewer raised that under EU copyright, functionality
and programming language are not protected expression, only the specific code is.
Our reading is that this does not defeat the clause, because it binds a
**licensee by contract**, and contract can reach further than copyright — the
real limit being someone who never accepted the terms at all, against whom we
have copyright only. **Is that reading correct?** And is the clause vulnerable on
competition-law or unfair-terms grounds rather than copyright grounds?

**B4. Does "any other product or service the licensor provides using the
software" cover a future managed offering?**
We dropped the reference to affiliates on the advice that it made the scope
unknowable to a reader. We want to be sure what remains still protects a hosted
or managed Lighthouse offering if we ever ship one.

**B5. Is the future-product paragraph a protection or a liability?**
It says that if *we* later bring a customer's non-competing product into
competition, that product does not thereby become a violation, the customer may
keep using versions released before that point, but not later ones. We believe
this protects the customer — without it a release of ours could put them in
breach overnight. One reviewer read it as customer-hostile instead. Which is it,
and does it read the way we intend?

---

## C. Section 2 — the AI clause, which has no precedent

**C1. Does it do anything a court would read?**
Section 2 says a prohibited result stays prohibited however it was produced —
by hand, by an automated system, or where the software reached that system as
input, context or training data. No mainstream software licence carries a clause
like this (ELv2, BUSL, FSL, PolyForm and SSPL are all silent; the AI restrictions
that exist are in model licences and content contracts). Is it enforceable,
merely declaratory, or actively unhelpful?

**C2. Is the training-data limb sound?**
Restricting use of licensed code as training data is the most novel part. Does
that restriction hold against a licensee? Does it interact badly with any
text-and-data-mining exception (we are thinking of the EU DSM Directive Art. 4,
which can be reserved by the rightsholder — is this document an effective
reservation, and should it say so explicitly)?

**C3. Does it sweep too far?**
Section 2 carries an express carve-out that it adds no restriction on tools, and
that a customer may use a code assistant to modify their own instance exactly as
they may modify it by hand. Is that carve-out effective, or could section 2 still
be read to void something section 1 deliberately permits?

---

## D. Housekeeping

**D1. Do past contributors need to agree?**
Seven commits from two outside contributors landed under MIT with no CLA. Our
understanding is that MIT's sublicensing right means we need nothing from them to
license future versions differently, and that their contributions remain MIT in
the versions already released. Please confirm — this is the classic relicensing
trap and we would rather be certain.

**D2. Is the version boundary stated unambiguously?**
`LICENSE` and `NOTICE` both say these terms apply to "every version released
after v26.9.9.9", and `NOTICE` preserves the MIT text in full for earlier
versions and states that existing forks keep their MIT rights. Four public forks
exist, all taken before the change. Is anything else needed to make the
non-retroactivity airtight?

**D3. Should worked examples be in the licence or the FAQ?**
A reviewer asked for concrete "allowed / not allowed" examples. We plan to put
them in our public documentation and have the Additional Terms point at them
while stating they are guidance and not part of the terms — following ELv2's own
FAQ, which carries the hosted-service examples outside the licence. Is that the
right split, or should examples be binding?

**D4. Does this licence need to reference our paid Terms and Conditions?**
Premium features are unlocked by a signed licence key sold under a separate
commercial agreement. The two documents here say nothing about that agreement,
and it says nothing about these. Is silence correct, or should they cross-refer?

---

## What we are **not** asking

Whether ELv2's text may be adapted and republished under another name. We
concluded there is no grant to do so and designed around it — hence two
documents. If you think that conclusion is wrong and adapting the text is
straightforward, say so, because a single self-contained document would be
simpler to read.
