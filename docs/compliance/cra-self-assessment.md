---
title: CRA Self-Assessment
layout: home
parent: Compliance
nav_order: 6
---

# CRA Self-Assessment Checklist

This checklist documents the self-assessment of Lighthouse against the essential cybersecurity requirements of the EU Cyber Resilience Act (CRA) Annex I.

## Assessment Information

| Field | Value |
|-------|-------|
| **Product** | Lighthouse |
| **Version Assessed** | All versions from implementation date forward |
| **CRA Classification** | Standard product with digital elements |
| **Assessment Date** | 2025-12-30, rows 1.3 and 1.5 re-evidenced 2026-08-17 |
| **Assessor** | Benjamin Huser-Berta (CRA Compliance Owner) |
| **Next Review** | 2026-12-30 |

## Part I: Security Requirements for Products with Digital Elements

### 1. Security by Design and Default

| Req | Requirement | Status | Evidence / Notes |
|-----|-------------|--------|------------------|
| 1.1 | Products shall be designed, developed, and produced to ensure an appropriate level of cybersecurity | ✅ Implemented | Secure coding practices, code review, SonarCloud analysis |
| 1.2 | Products shall be delivered without known exploitable vulnerabilities | ✅ Implemented | Dependency scanning via Dependabot, pre-release testing |
| 1.3 | Products shall be delivered with a secure by default configuration | ✅ Implemented | An installation encrypts stored credentials under a key of its own **without being configured to**: on first start it generates one and keeps it beside the database (`EncryptionKeyRingBootstrapper`). An operator can supply the key instead, through configuration or a mounted secret file, and the key published with the product is **rejected** as an active key rather than accepted — an instance that would fall back to it refuses to start once anything is stored. HTTPS enabled, security.txt. Evidence: [Encryption Key](../Installation/configuration.html#encryption-key), [Security](../security.html) |
| 1.4 | Products shall ensure protection from unauthorized access | ✅ Implemented | HTTPS by default, token encryption at rest |
| 1.5 | Products shall protect the confidentiality of data | ✅ Implemented | Work tracking credentials and OAuth tokens are stored as authenticated envelopes (AES-256-GCM, per-value nonce, key identifier bound as associated data) and are never returned to the browser for any role. Values that need no read-back are hashed rather than encrypted: API keys with PBKDF2-SHA256 and a per-key salt, embed session secrets with SHA-256. Downloaded database backups are encrypted as a whole under an operator-supplied password. The key can be rotated from inside the product without re-entering any credential, and a value that cannot be read is reported rather than overwritten. Scope and limits: [Security](../security.html) |
| 1.6 | Products shall protect the integrity of data | ✅ Implemented | Database integrity, input validation |
| 1.7 | Products shall process only data necessary for intended purpose | ✅ Implemented | Minimal data collection, no telemetry |
| 1.8 | Products shall protect availability and minimize negative impact | ✅ Implemented | Self-hosted architecture, user controls availability |
| 1.9 | Products shall minimize negative impact on other devices/networks | ✅ Implemented | No outbound connections except to configured work tracking systems |

### 2. Vulnerability Handling

| Req | Requirement | Status | Evidence / Notes |
|-----|-------------|--------|------------------|
| 2.1 | Identify and document vulnerabilities, including dependencies | ✅ Implemented | Dependabot alerts, SonarCloud, SBOM generation |
| 2.2 | Address vulnerabilities without delay | ✅ Implemented | Security update policy with defined timelines |
| 2.3 | Apply effective and regular tests and reviews | ✅ Implemented | CI/CD testing, code review process |
| 2.4 | Publicly disclose information about fixed vulnerabilities | ✅ Implemented | Release notes, GitHub Security Advisories |
| 2.5 | Provide a mechanism for sharing vulnerability information | ✅ Implemented | security@letpeople.work, SECURITY.md |
| 2.6 | Provide mechanisms for security updates | ✅ Implemented | GitHub Releases, Docker images, in-app update check |
| 2.7 | Security updates shall be available for expected lifetime | ✅ Implemented | Latest version always supported |

### 3. Software Bill of Materials (SBOM)

| Req | Requirement | Status | Evidence / Notes |
|-----|-------------|--------|------------------|
| 3.1 | Maintain SBOM covering components and dependencies | ✅ Implemented | SBOM generation workflow in ci_sbom.yml |
| 3.2 | SBOM in commonly used, machine-readable format | ✅ Implemented | CycloneDX |
| 3.3 | SBOM available to users | ✅ Implemented | Attached to GitHub Releases as Lighthouse-SBOM.zip |

## Part II: Vulnerability Handling Requirements for Manufacturers

### 1. Vulnerability Handling Process

| Req | Requirement | Status | Evidence / Notes |
|-----|-------------|--------|------------------|
| 1.1 | Establish and maintain documented vulnerability handling process | ✅ Implemented | PSIRT Process documentation |
| 1.2 | Provide contact point for vulnerability reports | ✅ Implemented | security@letpeople.work |
| 1.3 | Take appropriate remediation measures | ✅ Implemented | Security update policy |
| 1.4 | Apply effective policies for coordinated disclosure | ✅ Implemented | SECURITY.md, 90-day disclosure policy |

### 2. Documentation and Transparency

| Req | Requirement | Status | Evidence / Notes |
|-----|-------------|--------|------------------|
| 2.1 | Provide users with clear security information | ✅ Implemented | Security documentation, update policy |
| 2.2 | Provide information on how to report vulnerabilities | ✅ Implemented | SECURITY.md, docs |
| 2.3 | Provide information on update mechanisms | ✅ Implemented | Documentation, release notes |

## Assessment Summary

| Category | Total | ✅ Implemented | 🔄 In Progress | ❌ Not Started |
|----------|-------|----------------|----------------|----------------|
| Security by Design | 9 | 9 | 0 | 0 |
| Vulnerability Handling | 7 | 7 | 0 | 0 |
| SBOM | 3 | 3 | 0 | 0 |
| Manufacturer Obligations | 7 | 7 | 0 | 0 |
| **Total** | **26** | **26** | **0** | **0** |

## Open Items

**No open items.** All CRA Annex I requirements have been implemented.

## Conclusion

Based on this self-assessment, Lighthouse **fully meets** the essential cybersecurity requirements of CRA Annex I for a standard product with digital elements.

**Status**: Ready for Declaration of Conformity signature.

---

**Document Version**: 1.1  
**Last Updated**: 2026-08-17  
**Next Review**: 2026-12-30
