# Appendix C — what the earlier rounds found

**Read this only after you have formed your own view.** It exists so you can tell
us what you found that is *not* here, and what is here that you would dispute.

Three rounds, two reviewers, neither of them lawyers.

## Round 1

| Found | What we did |
|---|---|
| Section 1 contradicted its own carve-out: it said products compete "even when provided only within a single organization", while the carve-out said you may run a modified copy inside your own organization. | Rewrote. The line now sits between *your deployment of the software* and *a separate product built out of it*, rather than between internal and external use. |
| Section 1 reached the licensor's *affiliates* — a scope a reader cannot determine. | Dropped affiliates. It now reaches the software and other products the licensor provides using it. |
| The AI clause was ~90 words of machinery for a one-sentence rule. | Cut to the one sentence and retitled it to what it says. |
| A `<FILL AT RELEASE>` placeholder named the first source-available version; one reviewer proposed filling it from CI at tag time. | Removed the placeholder entirely. The boundary now reads "every version released after v26.9.9.9", which is determinate today. CI-filling would have left the committed `LICENSE` permanently incomplete. |
| Users will not know where the internal-use boundary lies; add worked examples. | Accepted, but the examples go in the public documentation as non-binding guidance, not in the licence — following the model Elastic uses for ELv2's own FAQ. |
| Never describe the result as open source, OSI-approved, "Elastic License 2.0", or equivalent to MIT. | Already the position; now stated in `LICENSE` itself. |

## Round 2

| Found | What we did |
|---|---|
| **Incorporation by reference is a real risk.** The additional terms were in a second file that `LICENSE` pointed at. A package manager, or a reader who opens only `LICENSE`, may never see it and can argue lack of notice. | **Restructured to one file.** Our terms became Part 1, above ELv2. The second file is gone. Neither reviewer had noticed that embedding was possible without touching Elastic's text. |
| Grant-and-restrict is weaker than a narrower grant: a breached covenant is contract, a use outside scope is infringement. | Added a scope sentence saying the ELv2 grant does not authorize the prohibited uses. |
| ELv2's Termination clause fires on violating "these terms", meaning ELv2's own — so breaching our terms may have triggered nothing. | Added an explicit linkage clause. |
| "A separate product that substitutes for it" was still fuzzy. | Defined it: primary purpose is to provide substantially the same core functionality, rather than to extend, integrate with, or operate the software. |
| No governing-law or jurisdiction clause anywhere — ELv2 has none and we had added none. | Added one as a candidate: Swiss law, Zurich forum, CISG excluded, subject to mandatory law. |

## Round 3

| Found | What we did |
|---|---|
| The scope sentence cannot manufacture an infringement remedy where the prohibited act implicates no copyright right — a cross-language rebuild being the obvious case. | Kept the scope sentence and added a second preserving contractual remedies where copyright does not reach. |
| The EU Software Directive makes certain user rights non-excludable — observe/study/test, back-up, decompilation for interoperability — and voids contrary contractual terms. | Added a savings clause (section 3) rather than arguing the point. |
| Section 1.1's exclusions missed tools that consume the software's output, and a broader product with merely incidental overlap. | Added both, plus customer-specific extensions. |
| Nothing stated how this licence relates to the paid commercial agreement. | Added section 4. See the unresolved disagreements below. |
| A machine-readable TDM reservation is needed alongside the licence text for the EU Art. 4(3) exception. | Moved into implementation scope, conditional on counsel. **We then found a limitation neither reviewer raised:** the W3C TDMRep mechanism is served from an origin's `/.well-known/` path, and we do not control `github.com`, which is where the source actually sits. A file on our own domains cannot cover the copy a crawler would find. |

## Two disagreements the earlier reviewers did not resolve

**1. Which instrument wins — the licence or the commercial agreement?**
One reviewer proposed that the commercial agreement *"shall control in the event
of any conflict"*. The other proposed that commercial terms *"do not modify this
License unless they expressly say so"*. We drafted the second, reasoning that a
private contract able to silently override the public licence invites the
argument that an enterprise agreement granted rights the source licence withholds,
with no reader of `LICENSE` able to detect it. **We would value a third view.**

**2. Do the historical outside contributors need to consent?**
One reviewer said MIT's sublicensing right does not permit relicensing their
code, and advised obtaining written consent or re-implementing the commits. The
other said MIT expressly grants "sublicense", contains no GPL-style
no-further-restrictions clause, and that the real question is chain of title
rather than the licence mechanism. **We would value a third view**, and note that
this is the one question where being wrong is expensive in both directions.

## One thing we corrected ourselves, which no reviewer caught

Our own internal record said "seven commits from two people". Measured from the
repository on 2026-09-11, it is **eight commits from three people** — a third
contributor appeared nowhere in the record, and about 450 of the three
contributors' lines survive on the current main branch, 59 of them in shipped
product code rather than tests or editor configuration. Every round up to that
point had reasoned about the contributor question from the wrong figures.

We mention it because it is the kind of thing a fresh reader is well placed to
catch, and because it is a fair warning that other stated facts in this document
may deserve checking rather than accepting.
