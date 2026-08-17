# Release lines this epic owes

Drafted with slice 06 (Story #5781), AC-6.8 and AC-6.20. **Not published here** — these go into the
release notes for the release that carries slices 01-09, because a release note names a version. No
security advisory accompanies them: maintainer decision V7, on the grounds that the key shipped in the
open in every build, so an advisory would describe the fix rather than a breach.

## The headline item — lead with the action

> **Your stored credentials now sit under a key that belongs to your instance.**
>
> Every Lighthouse before this release encrypted the tokens, passwords and OAuth credentials you gave
> it with one key — a key that ships inside the product and can be read out of the public source. From
> this release an instance makes and keeps a key of its own on first start, or takes one you supply, and
> the published key is refused as an active key.
>
> **What to do after upgrading:** open **Settings → Encryption** and press **Move stored secrets**. The
> screen tells you how many credentials are still on the old key, and moving them asks you to re-enter
> nothing. If it offers you no button, read the next two items — one of them is you.

## The sentence that must not be left out

> **Upgrading does not re-encrypt anything.** New credentials are written under the new key immediately,
> but everything you stored before the upgrade stays exactly where it was — under the key that is
> published with every copy of Lighthouse — until you run that one action. Until then, anyone holding a
> copy of Lighthouse can still read those values out of a copy of your database.

## ACTION REQUIRED — put this at the top, in a callout

Confirmed on a real deployment on 2026-08-17, not a hypothetical: a Docker Compose stack on Postgres,
in the shape the shipped example has, upgraded and came up on the published key with no way to fix it
from the interface.

> ## ⚠️ Action required if Lighthouse stores your data in Postgres, or in a container without a mounted data volume
>
> **Open Settings → Encryption after upgrading and read the first row.**
>
> If **Key source** says *the key published with the product*, this instance has nowhere to keep a key
> of its own — so it is protecting your stored credentials with the key that ships inside every copy of
> Lighthouse, and it will keep doing that for credentials you enter from now on. The screen offers you
> no *Move* or *Rotate* button in this state, and that is not a bug: a move needs a destination key and
> this instance has none. **Nothing is broken and nothing is lost — but nothing is protected either.**
>
> Two settings fix it; pick one and restart:
>
> | If you want | Set |
> |---|---|
> | Lighthouse to make and keep the key for you (recommended for Docker) | `Encryption__KeyStorePath` to a directory on a volume that outlives the container — on the official image, `/app/data/keys`, with `/app/data` on a named volume |
> | To own the key yourself | `Encryption__Key` to a 32-byte base64 key you keep somewhere safe |
>
> For a Compose file, that is one line in the `environment:` block:
>
> ```yaml
>       Encryption__KeyStorePath: /app/data/keys
> ```
>
> ```yaml
>     volumes:
>       - data:/app/data          # a named volume, not a host path
> ```
>
> Then restart, confirm **Key source** no longer says *published with the product*, and press
> **Move stored secrets**. Your credentials move across and nothing has to be re-entered.
>
> **Use a named volume, not a bind mount.** The container runs as a non-root user and cannot write into
> a host directory your own account owns. Kubernetes users are unaffected: the chart refuses to install
> without a key you supplied, so the situation cannot arise.

## The install that gets no button (finding E3)

> **If Lighthouse has nowhere to keep a key of its own, it stays on the published key — including for
> credentials you enter from now on.** That is an install on Postgres, or a container whose database file
> is not on a mounted volume, that upgrades without setting anything. It starts, keeps working, and warns
> on every start; and because there is no other key on the instance, **Settings → Encryption** offers it
> nothing to press. The remedy is one of two settings: `Encryption__Key` with a key of your own, or
> `Encryption__KeyStorePath` pointing at a directory on a volume that outlives the container. Both are on
> the [configuration page](https://docs.lighthouse.letpeople.work/Installation/configuration.html#when-lighthouse-has-nowhere-to-keep-a-key).

## Kubernetes operators

> **The chart now refuses to install without an encryption key** — `encryption.key`, or
> `encryption.existingSecret` if a secret store already owns it. In a cluster the application cannot own
> its key: it has no durable place to keep one and every replica would make a different one. Rotating is
> a sequence you drive, and Lighthouse never writes to your Secret. See
> [Kubernetes](https://docs.lighthouse.letpeople.work/Installation/kubernetes.html#the-encryption-key-the-cluster-owns).

## Supporting line — one address for the question

> **[docs.lighthouse.letpeople.work/security](https://docs.lighthouse.letpeople.work/security.html)** now
> answers "how do you look after the credentials I give you?" in one page: the plain answer first, then
> what is encrypted and how, what is hashed instead, who owns the key per deployment shape — and what
> none of it protects against. It is written to be forwarded to whoever asks you.

## Terminology check

These lines name Connections, credentials, and work tracking systems. No configurable term (feature,
work item, team, portfolio) appears, so nothing here needs to render in an installation's own
vocabulary.
