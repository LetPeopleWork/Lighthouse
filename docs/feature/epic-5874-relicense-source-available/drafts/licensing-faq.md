# Draft: the source-licence section for `docs/licensing/licensing.md`

**Status: draft, for slice 02. Do not publish before slice 01 lands** — this copy
describes terms that are not yet in force.

Two changes to that page:

1. **Replace the opening line.** It currently reads *"While Lighthouse is
   Open-Source and free to use in the basic version, there are certain features
   that require a license."* Replacement below.
2. **Insert the new section** below, immediately after the front matter and
   before `# About Licenses`.

A note on why this goes here at all: the page is currently about the **premium
licence key**, which is a different instrument from the **source-code licence**.
Counsel specifically wanted those kept separate. So the new section leads by
telling the reader there are two, which one it is about, and where the other one
is — otherwise this page quietly becomes the place where the two get confused.

---

## Replacement for the opening line of `# About Licenses`

```markdown
Lighthouse is free to use in the basic version, and certain features require a
license key. The license key can be acquired through the https://letpeople.work
website.
```

(The words "Open-Source" come out. The rest of the sentence is unchanged.)

---

## New section to insert

```markdown
# Two different licenses

Two separate things on this page are called a "license", and they are not
related:

- **The source code license** governs what you may do with Lighthouse's source
  code. It applies to everyone. That is the section immediately below.
- **The premium license key** is the `license.json` file you buy to unlock
  premium capabilities. Everything from *About Licenses* onwards is about that.

# The source code license

Lighthouse is **source available**. The source is public on GitHub, you can read
it, run it, audit it and change it for your own use. It is not open source under
the Open Source Initiative's definition, and it is not closed.

## What changed, and when

Lighthouse was published under the MIT License from 2025 until **v26.9.9.9**,
released on 9 September 2026. Every version up to and including that one
**remains MIT licensed in perpetuity** — that does not change, and it is not
retroactive. If you are running one of those versions, or hold a copy or a fork
of one, your rights under the MIT License are untouched.

Versions released **after** v26.9.9.9 are under the Lighthouse Source Available
License 1.0. The full text is in the
[LICENSE file](https://github.com/LetPeopleWork/Lighthouse/blob/main/LICENSE).

## What it means if you run Lighthouse

For almost everyone, nothing changes. You can still:

- run Lighthouse on your own infrastructure, for as many people in your
  organization as you like;
- read and audit every line of the source — which is what makes "your data never
  leaves your network" a checkable claim rather than a promise;
- change the code for your own needs, as much as you like, and run your modified
  version;
- use a code assistant to help you do it.

The restrictions are about what you may provide to **other people**, not about
what you do with your own instance.

## Allowed, and not allowed

These examples are guidance to help you find the boundary. They do not expand or
restrict the rights the LICENSE file grants — where an example and the LICENSE
disagree, the LICENSE governs.

**Allowed**

| You want to | Fine? |
|---|---|
| Run Lighthouse for your whole company from one instance | Yes |
| Modify Lighthouse heavily and run your own version internally | Yes |
| Use a code assistant or AI agent to make those modifications | Yes |
| Write a plugin or extension for Lighthouse | Yes |
| Integrate Lighthouse with another tool you use | Yes |
| Build a dashboard or report that consumes Lighthouse's output or API | Yes |
| Fork the repository and keep your changes to yourself | Yes |
| Read, study and test the source to understand how it works | Yes — and no licence can take that right away |
| Help a client install and run Lighthouse **on the client's own infrastructure** | Yes |

**Not allowed**

| Someone wants to | Why not |
|---|---|
| Offer Lighthouse, or a modified Lighthouse, as a hosted or managed service to others | It is a hosted service built on our software |
| Run Lighthouse **for** a client as a service you operate | Same reason — the client must run their own instance |
| Remove or bypass the license key check, or hide the features it protects | Named directly in the license |
| Remove or alter the licensing and copyright notices | Named directly in the license |
| Build a separate forecasting product out of Lighthouse's source that replaces it | It is a competing substitute — and this holds even if it is free, even if it is only used inside one company, and even if it is rewritten in a different language |
| Feed the source to an AI to produce any of the above | The result is what matters, not how it was produced |

**If you are not sure**, ask us at <https://letpeople.work#contact>. A question is
cheaper for both of us than a guess.

## Why we changed it

The short version: publishing the source is how we let you verify that
Lighthouse keeps your delivery data on your own infrastructure, and we want to
keep doing that. What we did not intend was to hand someone a complete
forecasting product to resell, or to make the paid tier trivially removable. The
MIT License permitted both. This one does not, and changes nothing else.

There is a free Community edition, and there always will be.
```

---

## Notes for whoever lands this

- The page's front matter (`title: Licensing`, `nav_order: 3`) stays as is.
- The two new `#` headings sit above the existing `# About Licenses`, so the
  premium-key content keeps its current structure and anchors. Existing links
  into `#licensed-features` and friends are unaffected.
- **The "Help a client install and run Lighthouse on the client's own
  infrastructure — Yes" row against "Run Lighthouse for a client as a service you
  operate — no" is the pair most likely to be read carelessly.** It is the real
  question a consultancy will have, and the decision behind it is recorded in the
  wave notes: partner hosting is not a motion LetPeopleWork wants, so ELv2's
  clause 1 restricts nothing intended.
- The claim-gate (`Scripts/check_license_claims.sh`) must pass after this lands.
  This draft deliberately contains no sentence calling Lighthouse open source.
