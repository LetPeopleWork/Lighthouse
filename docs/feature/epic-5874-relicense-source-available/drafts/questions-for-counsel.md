# Lighthouse relicensing — questions for counsel

**Prepared 2026-09-11, revised after two rounds of practitioner review.** Send
with two documents: `LICENSE.draft` and `NOTICE`.

Two rounds of non-legal review have already run. Everything they could settle has
been settled and folded into the draft; what follows is what they could not.

## What we are doing, in four sentences

Lighthouse is a self-hosted delivery-forecasting product from LetPeopleWork GmbH,
published on GitHub under the MIT License since 2025. We are moving future
versions to a source-available licence: the code stays public and inspectable,
customers may still read, run and modify it, but competing products are
restricted. We are **not** going closed source, and we are **not** seeking
OSI-approved status. Everything released up to and including v26.9.9.9
(2026-09-09) stays MIT permanently; the change binds later versions only.

The instrument is **one file in two parts**. `LICENSE` opens with a notice, then
Part 1 — the Lighthouse Additional Terms, which are ours — then Part 2, the
Elastic License 2.0 reproduced **verbatim and unaltered**. We chose that shape
deliberately: Elastic publishes ELv2 for others to adopt, but grants nothing
about publishing a *modified* version of its text, so we do not modify it. Our
restrictions sit above it rather than inside it.

---

## A. Structure — the questions that decide whether this works at all

**A1. Does the one-file structure hold?**
Our first draft put the Additional Terms in a second file incorporated by
reference. Both reviewers said that was the weakest point — a package manager or
a reader who opens only `LICENSE` may never see the second file and can argue
lack of notice. **We restructured: everything binding is now in `LICENSE`, with
our terms above Elastic's.** Does that resolve it, or does anything else need to
change in how the two parts are presented?

**A2. Scope versus covenant — and the remedy where copyright does not reach.**
Part 1 now carries two sentences rather than one:

> *"The license granted in Part 2 does not extend to, and expressly excludes, any
> use prohibited by these Additional Terms. A prohibited use falls outside the
> scope of the license granted, and is not licensed."*
>
> *"Where a prohibited use does not itself infringe copyright, the prohibition
> still binds you as a term of this license, and the licensor retains every
> contractual remedy for it."*

The first aims at an infringement remedy; the second exists because a reviewer
warned, correctly, that scope framing cannot manufacture infringement where the
underlying act implicates no copyright right — a cross-language rebuild being the
obvious case. **Do both sentences do what they are meant to, and does the second
weaken the first** by conceding that some prohibited uses are contract-only?

**A3. Reproducing Elastic's text under our own name — any exposure?**
We reproduce ELv2 unaltered as Part 2, state plainly that the composite is not
ELv2 and must not be identified as `Elastic-2.0`, that Part 1 restricts rights
ELv2 alone would grant, and that we are not affiliated with or endorsed by
Elasticsearch B.V. Is reproducing a third party's licence text inside our own
licence a copyright or trademark concern? Is our disclaimer adequate?

**A4. Confirm or replace our governing-law clause.**
There was none; we added one as a candidate (Part 1, section 4): Swiss
substantive law, conflict rules and CISG excluded, Zurich exclusive forum,
"to the extent that mandatory law does not require otherwise". We are a Swiss
GmbH distributing publicly and selling mostly into Europe. **Is that the right
clause, and is the mandatory-law carve-out adequate** for EU counterparties and
for anyone who might qualify as a consumer?

**A5. Confirm the termination linkage.**
ELv2's Termination clause fires on use "in violation of these terms", where
"these terms" reads as ELv2's own — so breaching our Part 1 arguably triggered
nothing. Part 1 section 3 now says a violation of the Additional Terms is a
violation of this licence for the purposes of that Termination section,
including its cure and reinstatement provisions. **Does that close it?**

---

## B. Scope — is section 1 drafted so people can actually apply it?

**B1. Is "a separate product that substitutes for it" determinate enough?**
Section 1 draws its line between a customer's own deployment of Lighthouse
(always permitted, however heavily modified) and a separate product built out of
it (not permitted, whether shipped externally or only deployed internally). A
first draft blurred this and a reviewer caught the contradiction. Is the revised
wording clear enough to be enforceable, or is it void for uncertainty? If the
latter, how would you draw the same line?

**B2. Does the internal-deployment restriction survive, and does it collide with
non-excludable statutory rights?**
We deliberately cover the case of an organisation building its own substitute
from our source and deploying it only to its own staff, never selling it. That is
the commercial risk we care most about — AI assistance has made it cheap.

  (a) Is a restriction on purely internal use enforceable, and does it create
      unfair-terms exposure in standard business terms under Swiss or German law?

  (b) The Software Directive makes certain user rights non-excludable and voids
      contrary contractual provisions — observing, studying and testing to
      determine underlying ideas and principles; back-up copies; decompilation
      for interoperability. **Does section 1 accidentally reach any of them?**
      Part 1 section 3 is our savings clause, drafted to make the question moot.
      **Is it adequate, or does it need to name the exceptions more precisely?**

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

**C2. The training-data limb, and the Art. 4(3) reservation — two questions, not
one.**
Restricting use of licensed code as training data is the most novel part.

  (a) **Contract.** Does the restriction hold against a licensee who accepted
      this licence?

  (b) **Statutory exception.** EU DSM Directive Art. 4 permits text and data
      mining of lawfully accessible works *unless the rightsholder has expressly
      reserved the use in an appropriate manner* — and for content made publicly
      available online, the Directive points to **machine-readable** means. One
      reviewer proposed we assert in the licence that this constitutes an Art.
      4(3) reservation. The other warned against self-certifying a mechanism we
      have not confirmed. **We took the cautious reading and assert nothing.**

So: **is a clause in `LICENSE` an effective Art. 4(3) reservation on its own?**

If not, we plan to publish a W3C TDMRep reservation — `/.well-known/tdmrep.json`
with `tdm-reservation` set and a `tdm-policy` URL. **There is a limitation we
cannot engineer around, and we would like your view on it:** TDMRep is served
from an origin's `/.well-known/` path. We control `letpeople.work` and
`docs.lighthouse.letpeople.work` and can publish there. **We do not control
`github.com`**, which is where the source actually lives and where a crawler
would encounter it.

  (a) Does a TDMRep file on our own domains reserve anything in respect of the
      copy hosted on GitHub?
  (b) If not, is there any mechanism that does — repository metadata, a file in
      the repository root, something in the licence text itself?
  (c) If nothing reaches the GitHub copy, is the reservation worth publishing at
      all, or does a partial reservation create a worse position than a clear
      contractual prohibition alone?

**C3. Does it sweep too far?**
Section 2 carries an express carve-out that it adds no restriction on tools, and
that a customer may use a code assistant to modify their own instance exactly as
they may modify it by hand. Is that carve-out effective, or could section 2 still
be read to void something section 1 deliberately permits?

---

## D. Housekeeping

**D1. Do past contributors need to agree? Our two reviewers disagreed, flatly.**
Seven commits from two outside contributors landed under MIT with no CLA.

One reviewer said MIT's sublicensing right does **not** permit relicensing their
code, and advised obtaining written consent or rewriting the seven commits,
reasoning that MIT "requires that the original MIT conditions (and lack of
additional restrictions) remain attached". Our reading is that this is wrong:
MIT has no no-further-restrictions clause — that belongs to GPL and CC-BY-SA —
and MIT expressly grants the right to "sublicense", which is the right to license
onward on different terms. The surviving obligation is retaining the copyright
and permission notice, which `NOTICE` does in full, and the already-released
versions stay MIT regardless.

The other reviewer agreed with that reading but said the real risk is **chain of
title**, not the licence mechanism.

So, two questions:

  (a) **Is our reading of MIT's sublicensing right correct**, such that no
      consent is needed and no code needs rewriting?

  (b) If so, what chain-of-title checks do you want on those seven commits —
      did the contributors own what they contributed, were they acting for an
      employer, did any commit carry third-party code, were any repository or
      contribution terms in force at the time?

This is the one question where a wrong answer is expensive in both directions:
chasing signatures we do not need, or shipping on rights we do not have.

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

**D4. Which instrument wins — the licence or the commercial agreement? Our
reviewers proposed opposite defaults.**
Premium features are unlocked by a signed licence key sold under a separate
commercial agreement. Part 1 section 4 now cross-refers, and we had to pick a
default:

  - One reviewer proposed the commercial agreement *"shall control in the event
    of any conflict"*.
  - The other proposed that commercial terms *"do not modify this License unless
    they expressly say so"*.

**We drafted the second**, because a private contract that silently overrides the
public licence invites the argument that an enterprise agreement granted rights
the source licence withholds, and no reader of `LICENSE` could detect it. Express
override stays available when we want it.

**Is that the right default**, and does section 4 as drafted achieve it without
undermining the commercial agreements we actually sign?

---

## What we are **not** asking

Whether ELv2's text may be adapted and republished under another name. We
concluded there is no grant to do so and designed around it — hence two
documents. If you think that conclusion is wrong and adapting the text is
straightforward, say so, because a single self-contained document would be
simpler to read.
