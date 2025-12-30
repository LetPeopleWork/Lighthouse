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
| **Assessment Date** | 2025-12-30 |
| **Assessor** | Benjamin Huser-Berta (CRA Compliance Owner) |
| **Next Review** | 2026-12-30 |

## Part I: Security Requirements for Products with Digital Elements

### 1. Security by Design and Default

| Req | Requirement | Status | Evidence / Notes |
|-----|-------------|--------|------------------|
| 1.1 | Products shall be designed, developed, and produced to ensure an appropriate level of cybersecurity | ✅ Implemented | Secure coding practices, code review, SonarCloud analysis |
| 1.2 | Products shall be delivered without known exploitable vulnerabilities | ✅ Implemented | Dependency scanning via Dependabot, pre-release testing |
| 1.3 | Products shall be delivered with a secure by default configuration | 🔄 In Progress | Reviewing default configurations for security hardening |
| 1.4 | Products shall ensure protection from unauthorized access | ✅ Implemented | HTTPS by default, token encryption at rest |
| 1.5 | Products shall protect the confidentiality of data | ✅ Implemented | Encrypted storage for sensitive data (tokens, credentials) |
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
| 3.1 | Maintain SBOM covering components and dependencies | 🔄 In Progress | SBOM generation workflow being implemented |
| 3.2 | SBOM in commonly used, machine-readable format | 🔄 In Progress | SPDX format selected |
| 3.3 | SBOM available to users | 🔄 In Progress | Will be attached to GitHub Releases |

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
| Security by Design | 9 | 8 | 1 | 0 |
| Vulnerability Handling | 7 | 7 | 0 | 0 |
| SBOM | 3 | 0 | 3 | 0 |
| Manufacturer Obligations | 7 | 7 | 0 | 0 |
| **Total** | **26** | **22** | **4** | **0** |

## Open Items

| Item | Description | Target Date | Owner |
|------|-------------|-------------|-------|
| 1 | Complete SBOM generation workflow | Q1 2026 | Engineering |
| 2 | Review and document secure-by-default configuration | Q1 2026 | Engineering |
| 3 | Attach SBOM to GitHub Releases | Q1 2026 | Engineering |
| 4 | Complete CRA Technical File | Q1 2026 | CRA Compliance Owner |

## Conclusion

Based on this self-assessment, Lighthouse substantially meets the essential cybersecurity requirements of CRA Annex I for a standard product with digital elements. The remaining items (SBOM integration, secure-by-default documentation) are in progress and targeted for completion in Q1 2026.

Upon completion of all items and final review, the EU Declaration of Conformity may be signed.

---

**Document Version**: 1.0  
**Last Updated**: 2025-12-30  
**Next Review**: 2026-12-30
