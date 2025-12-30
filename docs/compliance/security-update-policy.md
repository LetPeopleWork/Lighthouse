---
title: Security Update Policy
layout: home
parent: Compliance
nav_order: 3
---

# Security Update Policy

This document describes how Lighthouse handles security updates, including vulnerability response targets and user communication.

## Policy Scope

This policy applies to:
- The Lighthouse application (backend and frontend)
- All distribution channels (GitHub Releases, Docker images)
- All editions (Community and Premium)

## Important Notice

**These are best-effort targets, not contractual guarantees.**

Lighthouse is provided "as-is" under the MIT license. LetPeopleWork GmbH is a 2-person company with limited capacity. Response and fix timelines may vary based on:
- Complexity of the vulnerability
- Availability of team members
- Dependencies on third-party fixes
- Scope of required changes

See our [Terms and Conditions](https://letpeople.work/lighthouse#lighthouse-license) for the complete legal terms.

## Vulnerability Severity Classification

We use a simplified severity classification based on CVSS scores and exploitability:

| Severity | CVSS Range | Description |
|----------|------------|-------------|
| **Critical** | 9.0–10.0 | Immediate, severe impact; may allow remote code execution or complete system compromise |
| **High** | 7.0–8.9 | Significant impact; may allow unauthorized access or data exposure |
| **Medium** | 4.0–6.9 | Moderate impact; limited exposure or requires specific conditions |
| **Low** | 0.1–3.9 | Minor impact; informational or requires unlikely conditions |

### Actively Exploited

A vulnerability is considered "actively exploited" when there is credible evidence of exploitation in the wild. This triggers expedited handling regardless of CVSS score.

## Response Targets

### Acknowledgement and Triage

| Stage | Target Timeline |
|-------|-----------------|
| **Acknowledgement** | Within 5 business days of report |
| **Initial Triage** | Within 10 business days of report |

Triage includes:
- Reproduction of the issue
- Severity classification
- Decision on next steps (fix, defer, dispute, etc.)

### Fix Targets

| Severity | Target Fix Timeline | Notes |
|----------|---------------------|-------|
| **Actively Exploited** | Mitigation/workaround within 10 business days; fix within 30 calendar days | When feasible; may publish interim guidance |
| **Critical** | 60 calendar days | When feasible |
| **High** | 90 calendar days | When feasible |
| **Medium** | 180 calendar days | Often bundled into scheduled releases |
| **Low / Informational** | As time permits | Addressed when touching the affected area |

## Communication

### How Users Are Notified

| Channel | Content |
|---------|---------|
| **Release Notes** | All security fixes are documented in release notes |
| **GitHub Security Advisories** | Used for significant vulnerabilities (when warranted) |
| **Documentation** | Links to security advisories from compliance documentation |

### What We Communicate

For each security fix, we aim to provide:
- Affected versions
- Fixed version
- Brief description of the vulnerability
- Mitigation steps (if applicable before upgrade)
- Credit to reporter (if desired)

## Coordinated Disclosure

We support coordinated disclosure:
- Reporters may request an embargo period (typically up to 90 days)
- We will coordinate disclosure timing with reporters
- We may request extensions if a fix requires more time

## Third-Party Dependencies

For vulnerabilities in third-party dependencies:
- We monitor via GitHub Dependabot alerts
- We update dependencies in accordance with this policy
- If upstream fixes are delayed, we may apply workarounds or document mitigations

## Unsupported Versions

We support only the **latest released version** of Lighthouse. Users on older versions should upgrade to receive security fixes.

| Version | Support Status |
|---------|----------------|
| Latest release | Supported |
| All previous releases | Not supported |

## Reporting a Vulnerability

To report a security vulnerability:

**Email**: [security@letpeople.work](mailto:security@letpeople.work)

Please include:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Your contact information (for follow-up)

See [SECURITY.md](https://github.com/LetPeopleWork/Lighthouse/blob/main/SECURITY.html) for complete reporting instructions.

---

**Document Version**: 1.0  
**Last Updated**: 2025-12-30  
**Next Review**: 2026-12-30
