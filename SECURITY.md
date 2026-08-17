# Security Policy

This file is the reporting path and the policy behind it. If what you want is **how Lighthouse protects
the credentials you give it** — what is encrypted, who owns the key, and what none of it protects against
— that is one page: <https://docs.lighthouse.letpeople.work/security.html>.

## Reporting a Vulnerability

We take the security of Lighthouse seriously. If you believe you have found a security vulnerability, please report it to us as described below.

### How to Report

**Email**: [security@letpeople.work](mailto:security@letpeople.work)

Please include the following information in your report:

- **Description**: A clear description of the vulnerability
- **Steps to Reproduce**: Detailed steps to reproduce the issue
- **Impact**: Your assessment of the potential impact
- **Affected Versions**: Which version(s) of Lighthouse are affected (if known)
- **Proof of Concept**: Code, screenshots, or other evidence (if available)
- **Your Contact Information**: So we can follow up with you

### What to Expect

| Stage | Timeline |
|-------|----------|
| **Acknowledgement** | Within 5 business days |
| **Initial Triage** | Within 10 business days |
| **Status Update** | At least every 30 days until resolution |

### Coordinated Disclosure

We support coordinated disclosure:

- We will work with you to understand and resolve the issue
- We request that you do not publicly disclose the vulnerability until we have had a chance to address it
- We will credit you in our security advisory (unless you prefer to remain anonymous)
- Standard embargo period: up to 90 days (negotiable based on severity and complexity)

### What We Ask

- **Do not** access or modify other users' data
- **Do not** perform actions that could harm the availability of the service
- **Do not** publicly disclose the vulnerability before coordinated disclosure
- **Do** provide sufficient information for us to reproduce and fix the issue

## Supported Versions

We provide security updates for the **latest released version only**.

| Version | Supported |
|---------|-----------|
| Latest release | ✅ Yes |
| Previous releases | ❌ No |

We recommend all users stay on the latest version to receive security fixes.

## Security Update Policy

For details on how we handle security updates, including response timelines and severity classification, see our [Security Update Policy](https://docs.lighthouse.letpeople.work/compliance/security-update-policy.html).

### Summary

- **Actively exploited / Critical**: Target fix within 30-60 calendar days
- **High**: Target fix within 90 calendar days
- **Medium**: Target fix within 180 calendar days
- **Low**: Addressed as time permits

These are best-effort targets. Lighthouse is provided "as-is" under the MIT license. See our [Terms and Conditions](https://letpeople.work/lighthouse#lighthouse-license) for complete legal terms.

## Security Contacts

| Role | Contact |
|------|---------|
| **Security Reports** | security@letpeople.work |
| **PSIRT Contact (Main)** | Benjamin Huser-Berta |
| **PSIRT Contact (Backup)** | Peter Zylka-Greger |

## Scope

This security policy covers:

- The Lighthouse application (backend and frontend)
- Official Docker images (`ghcr.io/letpeoplework/lighthouse`)
- Official release artifacts from GitHub Releases

This policy does **not** cover:

- Third-party integrations (Jira, Azure DevOps, Linear)
- User-deployed infrastructure or configurations
- Forks or modified versions of Lighthouse

## Encryption of stored credentials

Up to and including the releases before secret-encryption key custody shipped, Lighthouse encrypted
stored credentials with a key that was written into the product and is readable in this repository.
That was documented behaviour rather than a defect — the key was never withheld — but it meant every
install that had not overridden the key shared one, and the documented override read a setting the code
did not. Both are fixed:

- An instance now generates and keeps a key of its own on first start, or takes one you supply through
  configuration or a mounted secret file. The published key is **refused** as an active key.
- The key can be rotated from Settings → Encryption without re-entering a single credential, and a
  credential that cannot be read is named rather than overwritten.
- **Upgrading re-encrypts nothing.** Anything stored before the upgrade stays under the previously
  published key until a rotation is run, and the product says so on its own.

No security advisory was published for this, deliberately: the key shipped in the open in every build, so
an advisory would describe the fix rather than a breach. What an operator needs is the upgrade and the
rotation, and the release notes carry both.

## Acknowledgments

We appreciate the security research community and will acknowledge researchers who report valid vulnerabilities (with their permission) in our release notes and security advisories.

---

**Last Updated**: 2026-08-17
