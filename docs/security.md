---
title: Security
layout: home
nav_order: 4
---

# Security

This page is the answer to "how does Lighthouse look after the credentials I give it?" — written so that
the person asking can read the top of it, and the security person they forward it to can read the rest.

## The short answer

Lighthouse needs credentials for your work tracking systems, because forecasts stop being current the
moment updating stops. So it stores them, and this is what happens to them:

- **They are encrypted before they are written to the database.**
- **They are never sent back to the browser** — not to you, not to an administrator, not to anyone,
  whatever they are allowed to do.
- **They never leave your instance.** Lighthouse is software you run. It talks to the work tracking
  systems you configured and to nothing else.
- **You can revoke one at the source at any time** — in Jira, Azure DevOps, ServiceNow, Linear — and
  Lighthouse loses that access immediately, without asking anyone's permission.

If you found a vulnerability, please email [security@letpeople.work](mailto:security@letpeople.work).
[What to expect](#reporting-something) is below.

---

## Verify our claims

Everything above is checkable. Lighthouse is open source, so the paragraphs below name what to look for.

### What is stored, and what is done to it

| What | How it is stored |
| --- | --- |
| Work tracking system credentials — personal access tokens, API keys, passwords, client secrets | Encrypted. AES-256-GCM, one nonce per value, and the identifier of the key used is written into the record so a wrong key is detected rather than guessed at. |
| OAuth access and refresh tokens | The same. |
| API keys you create *in* Lighthouse | **Not encrypted — hashed**, with PBKDF2-SHA256, 100 000 iterations and a salt per key, compared in constant time. Hashing is the stronger choice here: nothing needs to read the key back, so nothing can. That is why a new API key is shown to you exactly once. |
| Embed session secrets | Hashed with SHA-256, for the same reason. |
| Database backups you download from Lighthouse | Encrypted as a whole, with AES-256 and PBKDF2-SHA256 at 100 000 iterations, under the password you supply when you create the backup. |
| Everything else — teams, portfolios, work item history, forecasts, settings | Stored as it is. None of it is a secret, and treating it as one would only make it look protected. |

Two consequences worth being explicit about. A credential is **write-only** from the interface's point
of view: you can replace one, you cannot read one back. And a database backup contains your credentials
in their encrypted form, so it is exactly as sensitive as the key that opens them.

### The key, and who owns it

There is one encryption key in force at a time, and Lighthouse can hold older keys alongside it so that
nothing already stored becomes unreadable. Where that key comes from is the part you decide:

| Custody | What it means |
| --- | --- |
| **Lighthouse made it** | On first start, Lighthouse creates a key for itself and keeps it in a folder next to the database. Standalone installs and Docker with a data volume work this way. Back that folder up with the database — losing one without the other leaves the credentials unreadable. |
| **You supplied it** | Set `Encryption__Key` (or `Encryption__Keys`) and the key is yours. Lighthouse will never make one of its own while it is set, and never writes to wherever you keep it. |
| **A secret store supplied it** | Point `Encryption__KeysFile` at a mounted file — a Kubernetes Secret, an external secrets operator. Same as above, kept somewhere else. The Helm chart **refuses to install** without one of the two. |

Lighthouse never writes to the place a key came from. That is what makes the Kubernetes flow safe: you
add a key to your Secret, Lighthouse reads it, and nothing in the cluster grants the application
permission to change it back.

**Rotation costs no credentials.** Settings → Encryption, System Administrator only, moves every stored
credential onto a new key without asking anyone to re-enter anything, and reports each one it could not
read rather than overwriting it. The full procedure — for both the key Lighthouse owns and the key you
own — is in [Configuration](Installation/configuration.html#changing-the-key-without-re-entering-a-single-credential),
and the screen itself is described in [Secret Encryption Key](settings/encryption.html).

**Upgrading re-encrypts nothing.** Versions before this one encrypted with a key that ships inside the
product and can be read out of the public source. Upgrading makes an instance stop *writing* under that
key; it does not move what is already stored. Until a rotation is run, everything stored earlier is
still readable by anyone holding a copy of Lighthouse. The encryption screen says so on its own, without
being asked, and tells you how many credentials it applies to.

### What this does not protect against

Volunteering the limits is the part that makes the rest of this page worth reading.

- **Anyone who holds the key.** Encryption at rest protects a database that has been taken without its
  key. Someone with both has your credentials, and no amount of cryptography changes that.
- **Anyone with access to the host.** Shell access to the machine or container running Lighthouse reaches
  the database and the key store together. Protect the host as you would protect the credentials.
- **An administrator with access to the host** — as opposed to one using the interface. Through
  Lighthouse, a System Administrator can replace a credential and rotate the key, and cannot read a
  stored credential back. That boundary is the interface's, not the operating system's.
- **A credential with more access than it needs.** Lighthouse cannot reduce the scope of the token you
  gave it. Issue read-only credentials where your work tracking system supports them, and revoke at the
  source rather than here.
- **An instance nobody has to sign in to.** Authentication is something you turn on — see
  [Authentication](Installation/authentication.html). Without it, anyone who can reach the address can
  use everything the interface offers.
- **A compromised work tracking system.** If the system Lighthouse reads from is compromised, the data
  Lighthouse shows you is too.
- **Your own copies.** Backups you download, exported data and screenshots leave Lighthouse's custody
  the moment they are created.

Lighthouse is also not a secrets manager: it stores the credential you gave it, and it does not rotate
that credential at the source.

### What leaves the instance

Nothing, unless you configure it. Lighthouse makes outbound connections to the work tracking systems you
configured, to check for a new version, and — only if you enable it — to the metrics and log endpoint you
name yourself, which carries operational telemetry and no credentials. There is no vendor collection and
no phone-home.

### Compliance material

- [CRA self-assessment](compliance/cra-self-assessment.html) — the requirement-by-requirement view,
  with the evidence for each row.
- [CRA technical file](compliance/cra-technical-file.html), [declaration of conformity](compliance/declaration-of-conformity.html).
- [Security update policy](compliance/security-update-policy.html) — severity classification and target
  timelines.
- [PSIRT process](compliance/psirt-process.html) and [roles and contacts](compliance/roles-and-contacts.html).

---

## Reporting something

Email [security@letpeople.work](mailto:security@letpeople.work) with what you found, how to reproduce it,
and which version you saw it on. Please give us a chance to fix it before publishing.

| Stage | Timeline |
| --- | --- |
| Acknowledgement | Within 5 business days |
| Initial triage | Within 10 business days |
| Status update | At least every 30 days until it is resolved |

We support coordinated disclosure, with an embargo of up to 90 days depending on severity, and we credit
reporters in the release notes unless you would rather stay anonymous. Security updates are provided for
the latest released version. The full policy, including scope and what we ask of you, is in
[SECURITY.md](https://github.com/letpeoplework/lighthouse/blob/main/SECURITY.md).
