# Lighthouse relicensing — brief for counsel

**LetPeopleWork GmbH · prepared 2026-09-11**

This is self-contained. Section 1 is the background, section 2 the questions,
and the two drafted documents are reproduced in full as Appendix A and
Appendix B. Nothing is in force: the repository is still MIT-licensed and no
part of this has been published.

## 1. What we are doing

Lighthouse is a self-hosted delivery-forecasting product, published on GitHub
under the MIT License since 2025. We are moving **future** versions to a
source-available licence. The code stays public and inspectable; customers may
still read, run and modify it; competing products are restricted. We are not
going closed source and we are not seeking OSI-approved status.

Everything released up to and including **v26.9.9.9 (9 September 2026)** stays
MIT permanently. The change binds later versions only.

The instrument is **one file in two parts**. `LICENSE` opens with a notice, then
Part 1 — the Lighthouse Additional Terms, which are ours — then Part 2, the
Elastic License 2.0 reproduced **verbatim and unaltered**. We chose that shape
deliberately: Elastic publishes ELv2 for others to adopt, but grants nothing
about publishing a *modified* version of its text, so we do not modify it. Our
restrictions sit above it rather than inside it.

The company is a Swiss GmbH. Distribution is public and free; revenue comes from
a signed licence key that unlocks premium features, sold under a separate
commercial agreement. Two outside contributors have made seven commits in the
project's history, under MIT, with no CLA.

## 2. What we need from you

Eight questions follow. They are the residue of three rounds of non-legal
review — everything those rounds could settle has been settled and drafted in.
We would value a view on all eight, but these four are the ones we consider
release-blocking:

- **A2** — does the scope-versus-covenant framing work, and does the contractual
  backstop weaken it?
- **A3** — is verbatim reproduction of ELv2 inside our own licence safe?
- **C2** — what constitutes an effective Art. 4(3) TDM reservation for source
  code hosted on a domain we do not control?
- **D1** — is the MIT chain of title sufficient to relicense seven historical
  commits, and what diligence do you want on them?

Where we have taken a position, we say so and say why, so you can disagree with
a stated reason rather than reconstruct one.

---

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

---

# Appendix A — LICENSE, complete and as drafted

This is the entire file. Part 2 (the Elastic License 2.0) is reproduced
byte-for-byte from Elastic's published text and is not edited in any way.

```text
Lighthouse Source Available License 1.0
SPDX-License-Identifier: LicenseRef-Lighthouse-SAL-1.0

Copyright (c) 2025-2026 LetPeopleWork GmbH

Lighthouse is source available. The source code is public and inspectable, and
you may run it, read it, and change it for your own use. It is not open source
under the Open Source Initiative's definition, and it is not closed: this file
tells you exactly what you may and may not do.

THIS LICENSE HAS TWO PARTS. Both are binding, both are in this file, and neither
stands alone:

  PART 1  The Lighthouse Additional Terms, immediately below. They limit the
          rights granted in Part 2.

  PART 2  The Elastic License 2.0, reproduced verbatim after Part 1, without
          alteration.

No rights are granted under this license except subject to both parts. Anyone
who receives any part of the software from you must receive this entire file.

WHAT LICENSE APPLIES TO WHICH VERSION

  Every version of Lighthouse up to and including v26.9.9.9, released
  2026-09-09, was published under the MIT License and remains licensed under
  the MIT License in perpetuity. Nothing in this file changes that, and nothing
  in this file applies retroactively to those versions. The full text of that
  MIT License is preserved in the NOTICE file.

  This license applies to every version of Lighthouse released after v26.9.9.9.

THIS IS NOT THE ELASTIC LICENSE 2.0. Part 2 is the Elastic License 2.0
reproduced verbatim, but the licensor of this software is LetPeopleWork GmbH,
not Elasticsearch B.V.; Part 1 restricts rights that the Elastic License 2.0
alone would grant; and this composite license is neither the Elastic License 2.0
nor endorsed by or affiliated with Elasticsearch B.V. Do not identify this
software as Elastic-2.0 licensed. Its identifier is
LicenseRef-Lighthouse-SAL-1.0.

================================================================================
  PART 1 - LIGHTHOUSE ADDITIONAL TERMS
================================================================================

These Additional Terms are part of this license. They are read together with the
Elastic License 2.0 reproduced in Part 2 below. Where the two parts conflict,
these Additional Terms govern.

The license granted in Part 2 does not extend to, and expressly excludes, any
use prohibited by these Additional Terms. A prohibited use falls outside the
scope of the license granted, and is not licensed.

Where a prohibited use does not itself infringe copyright, the prohibition still
binds you as a term of this license, and the licensor retains every contractual
remedy for it.

"The software", "the licensor", "you" and "your company" carry the meanings
given to them in Part 2.


1. NO COMPETING USE

You may not use the software to provide a product or service that competes with
the software, or with any other product or service the licensor provides using
the software.

You may not use the software to build a separate product that substitutes for
it. This applies whether you make that product available to others or deploy it
only inside your own organization.

A separate product substitutes for the software when its primary purpose is to
provide substantially the same core functionality as the software, rather than
to extend, integrate with, or operate the software.

Products and services compete even when they provide their functionality
through a different kind of interface, for a different technical platform, or in
a different programming language, and even when they are provided free of
charge. If you present a product as a practical substitute for the software, it
competes.

1.1 WHAT SECTION 1 DOES NOT RESTRICT

Running the software is always permitted. That includes running a modified
copy, changing it as much as you like, and serving everyone in your
organization from a single instance, for as long as you like.

None of the following is a separate substitute for the software: a plugin, an
extension, an integration, an extension written for your own needs, an
application built around the software, a tool that consumes its output, or a
modified deployment of the software itself. A broader product that happens to
contain some functionality overlapping with the software is not a substitute for
it unless providing substantially the same core functionality is that product's
primary purpose.

The line this section draws is between your deployment of the software and a
separate product built out of it.

1.2 IF THE LICENSOR LATER BRINGS YOUR PRODUCT INTO COMPETITION

If you are using the software to provide a product that does not compete, and
the licensor afterwards releases a new version of the software, or a new product
built using the software, and that release brings your product into competition
for the first time, then:

  - your product does not thereby become a violation of this license;
  - you may continue to use every version of the software released before that
    release to provide it; and
  - you may not use any version released after it to provide it.

This section exists to protect you. Without it, a release by the licensor could
put you in violation of this license overnight, through no act of your own.


2. PROHIBITED RESULTS STAY PROHIBITED HOWEVER THEY ARE PRODUCED

A result that this license prohibits is prohibited regardless of how it was
produced. It makes no difference whether a person wrote it by hand, whether an
automated system produced it, or whether the software reached that system as
input, as context, or as training data.

2.1 WHAT SECTION 2 DOES NOT RESTRICT

Section 2 adds no restriction on tools. You may use any tool you like -
including a code assistant, a coding agent, or a model - to read, understand or
change the software for any purpose this license permits. Using a code assistant
to modify your own instance is permitted for exactly the same reason that
modifying it by hand is permitted.

Section 2 closes a route to the results section 1 prohibits. It does not narrow
anything section 1 permits.


3. RIGHTS THAT CANNOT BE RESTRICTED

Nothing in this license restricts any right you hold under mandatory law that
cannot be excluded by agreement. Where applicable, that includes your statutory
rights to observe, study or test the functioning of the software in order to
determine the ideas and principles underlying it, to make a back-up copy, and to
decompile the software so far as necessary to achieve interoperability with
other programs.

Where a provision of this license would restrict such a right, that provision
does not apply to the extent of the restriction, and the rest of this license
continues in force.


4. RELATIONSHIP TO COMMERCIAL AGREEMENTS

Some features of the software are enabled by a license key issued by the
licensor, and their use may also be governed by a separate written commercial
agreement between you and the licensor. That agreement and this license are
separate instruments. A commercial agreement does not modify this license unless
it expressly says that it does.


5. TERMINATION

A violation of these Additional Terms is a violation of this license for the
purposes of the Termination section of the Elastic License 2.0 in Part 2,
including that section's cure and reinstatement provisions.


6. GOVERNING LAW AND JURISDICTION

This license is governed by the substantive law of Switzerland, excluding its
conflict-of-law rules and excluding the United Nations Convention on Contracts
for the International Sale of Goods. The courts of Zurich, Switzerland have
exclusive jurisdiction over any dispute arising out of or in connection with
this license, to the extent that mandatory law does not require otherwise.


For worked examples of what this license allows and does not allow, see the
licensing page in the Lighthouse documentation. Those examples are guidance.
They do not expand or restrict the rights granted by this license.

================================================================================
  PART 2 - ELASTIC LICENSE 2.0, REPRODUCED VERBATIM

  Everything below this block is the Elastic License 2.0, reproduced without
  modification from https://www.elastic.co/licensing/elastic-license.
  Nothing in it has been added, removed, reworded or reordered.
================================================================================

Elastic License 2.0

URL: https://www.elastic.co/licensing/elastic-license

## Acceptance

By using the software, you agree to all of the terms and conditions below.

## Copyright License

The licensor grants you a non-exclusive, royalty-free, worldwide,
non-sublicensable, non-transferable license to use, copy, distribute, make
available, and prepare derivative works of the software, in each case subject to
the limitations and conditions below.

## Limitations

You may not provide the software to third parties as a hosted or managed
service, where the service provides users with access to any substantial set of
the features or functionality of the software.

You may not move, change, disable, or circumvent the license key functionality
in the software, and you may not remove or obscure any functionality in the
software that is protected by the license key.

You may not alter, remove, or obscure any licensing, copyright, or other notices
of the licensor in the software. Any use of the licensor’s trademarks is subject
to applicable law.

## Patents

The licensor grants you a license, under any patent claims the licensor can
license, or becomes able to license, to make, have made, use, sell, offer for
sale, import and have imported the software, in each case subject to the
limitations and conditions in this license. This license does not cover any
patent claims that you cause to be infringed by modifications or additions to
the software. If you or your company make any written claim that the software
infringes or contributes to infringement of any patent, your patent license for
the software granted under these terms ends immediately. If your company makes
such a claim, your patent license ends immediately for work on behalf of your
company.

## Notices

You must ensure that anyone who gets a copy of any part of the software from you
also gets a copy of these terms.

If you modify the software, you must include in any modified copies of the
software prominent notices stating that you have modified the software.

## No Other Rights

These terms do not imply any licenses other than those expressly granted in
these terms.

## Termination

If you use the software in violation of these terms, such use is not licensed,
and your licenses will automatically terminate. If the licensor provides you
with a notice of your violation, and you cease all violation of this license no
later than 30 days after you receive that notice, your licenses will be
reinstated retroactively. However, if you violate these terms after such
reinstatement, any additional violation of these terms will cause your licenses
to terminate automatically and permanently.

## No Liability

*As far as the law allows, the software comes as is, without any warranty or
condition, and the licensor will not be liable to you for any damages arising
out of these terms or the use or nature of the software, under any kind of
legal claim.*

## Definitions

The **licensor** is the entity offering these terms, and the **software** is the
software the licensor makes available under these terms, including any portion
of it.

**you** refers to the individual or entity agreeing to these terms.

**your company** is any legal entity, sole proprietorship, or other kind of
organization that you work for, plus all organizations that have control over,
are under the control of, or are under common control with that
organization. **control** means ownership of substantially all the assets of an
entity, or the power to direct its management and policies by vote, contract, or
otherwise. Control can be direct or indirect.

**your licenses** are all the licenses granted to you for the software under
these terms.

**use** means anything you do with the software requiring one of your licenses.

**trademark** means trademarks, service marks, and similar rights.
```

---

# Appendix B — NOTICE, complete and as drafted

```text
Lighthouse
Copyright (c) 2025-2026 LetPeopleWork GmbH

CURRENT LICENSE
---------------

Every version of Lighthouse released after v26.9.9.9 is licensed under
the Lighthouse Source Available License 1.0
(SPDX: LicenseRef-Lighthouse-SAL-1.0). The complete license is in the LICENSE
file. It has two parts, both in that one file and both binding:

  Part 1   the Lighthouse Additional Terms, which limit the rights granted in
           Part 2
  Part 2   the Elastic License 2.0, reproduced verbatim and unaltered

The source code remains public and inspectable. This is not a move to closed
source, and OSI-approved status is not claimed.


PRIOR VERSIONS REMAIN UNDER THE MIT LICENSE, PERMANENTLY
--------------------------------------------------------

Every version of Lighthouse up to and including v26.9.9.9, released 2026-09-09,
was published under the MIT License. Those versions remain licensed under the
MIT License in perpetuity. The license change is not retroactive: it binds later
versions only.

If you hold a copy of any of those versions — including a fork taken from the
repository before the change — your rights under the MIT License are unaffected
by anything in the LICENSE file, and nothing in it asks you to give them up.

The MIT License that applied to those versions, preserved in full:

    MIT License

    Copyright (c) 2025 LetPeopleWork GmbH

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.


THIRD-PARTY COMPONENTS
----------------------

Lighthouse bundles third-party open-source components, each under its own
license. The complete inventory, with versions and licenses, is generated per
release as a CycloneDX SBOM and is readable in the running application under
Settings > System Info > OSS Attribution.
```
