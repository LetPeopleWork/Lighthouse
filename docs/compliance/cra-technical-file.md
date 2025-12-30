---
title: CRA Technical File
layout: home
parent: Compliance
nav_order: 5
---

# CRA Technical Documentation File

This is the Technical Documentation File for Lighthouse as required by the EU Cyber Resilience Act (CRA) Article 31 and Annex VII.

## Document Control

| Field | Value |
|-------|-------|
| **Product** | Lighthouse |
| **Manufacturer** | LetPeopleWork GmbH |
| **Document Version** | 1.0 |
| **Created** | 2025-12-30 |
| **Last Updated** | 2025-12-30 |
| **Retention Period** | 10 years after last distribution |

---

## Table of Contents

1. [Product Description](#1-product-description)
2. [Intended Use](#2-intended-use)
3. [Architecture Overview](#3-architecture-overview)
4. [Data Flows](#4-data-flows)
5. [Risk Assessment](#5-risk-assessment)
6. [Security Controls](#6-security-controls)
7. [Software Bill of Materials](#7-software-bill-of-materials)
8. [Update Mechanism](#8-update-mechanism)
9. [Testing and Verification](#9-testing-and-verification)
10. [Vulnerability Handling](#10-vulnerability-handling)
11. [Conformity Assessment](#11-conformity-assessment)
12. [References](#12-references)

---

## 1. Product Description

### 1.1 Product Identity

| Field | Value |
|-------|-------|
| **Name** | Lighthouse |
| **Type** | Software application |
| **CRA Classification** | Standard product with digital elements |
| **Current Version** | See [GitHub Releases](https://github.com/LetPeopleWork/Lighthouse/releases) |
| **Version Scheme** | vYY.MM.DD.build (date-based) |

### 1.2 Overview

Lighthouse is a self-hosted application that provides:
- Flow metrics tracking (WIP, Cycle Time, Throughput, Work Item Age)
- Monte Carlo simulation-based delivery forecasts
- Portfolio and team management
- Integration with work tracking systems (Jira, Azure DevOps, Linear, CSV)

### 1.3 Supported Platforms

| Platform | Distribution Format |
|----------|---------------------|
| Windows x64 | ZIP archive (signed executable) |
| Linux x64 | ZIP archive |
| macOS (Universal) | DMG / APP.ZIP (signed and notarized) |
| Docker | Container image (linux/amd64, linux/arm64) |

### 1.4 Editions

Lighthouse is distributed as a single binary with two editions:
- **Community Edition**: Free, feature-limited
- **Premium Edition**: License key required, full features

The editions share the same codebase and security properties.

---

## 2. Intended Use

### 2.1 Primary Use Cases

- Software delivery forecasting using probabilistic methods
- Team performance visibility via flow metrics
- Portfolio-level delivery tracking and planning
- Integration with existing work tracking tools

### 2.2 Target Users

- Software development teams and managers
- Agile coaches and Scrum Masters
- Portfolio and program managers
- Engineering leadership

### 2.3 Deployment Model

Lighthouse is designed for **self-hosted deployment**:
- Users download and run on their own infrastructure
- Users are responsible for network security and access control
- No cloud/SaaS version is offered by the manufacturer

### 2.4 Exclusions

Lighthouse is **not** intended for:
- Critical infrastructure protection
- Safety-critical systems
- Medical, automotive, or aerospace applications
- Government classified information systems

---

## 3. Architecture Overview

### 3.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        User Browser                         │
│                    (React Frontend)                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Lighthouse Backend                       │
│                    (ASP.NET Core)                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │   REST API  │  │   MCP API   │  │  Background Services│  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│                          │                                  │
│                          ▼                                  │
│                    ┌───────────┐                            │
│                    │  Database │                            │
│                    │ SQLite/PG │                            │
│                    └───────────┘                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS (outbound only)
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                 External Work Tracking Systems              │
│         (Jira, Azure DevOps, Linear, CSV Import)            │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | React 19, TypeScript, Material-UI |
| Backend | .NET 10, ASP.NET Core |
| Database | SQLite (default) or PostgreSQL |
| Authentication | Token-based (user-configured) |
| Communication | HTTPS (TLS 1.2+) |

### 3.3 External Integrations

| System | Connection Type | Purpose |
|--------|-----------------|---------|
| Jira | REST API (HTTPS) | Work item synchronization |
| Azure DevOps | REST API (HTTPS) | Work item synchronization |
| Linear | GraphQL API (HTTPS) | Work item synchronization |
| CSV | File import | Work item import |
| GitHub | REST API (HTTPS) | Update checking |

---

## 4. Data Flows

### 4.1 Data Classification

| Data Type | Classification | Storage |
|-----------|----------------|---------|
| Work item metadata | User Data | Database |
| Team/Portfolio configuration | User Data | Database |
| API tokens for integrations | Sensitive | Database (encrypted) |
| User settings | User Data | Database |
| Application logs | Operational | Local filesystem |

### 4.2 Data Flow Diagram

See architecture diagram in Section 3. Key flows:

1. **User → Frontend → Backend**: User interactions, configuration
2. **Backend → External Systems**: Work item synchronization (outbound only)
3. **Backend → Database**: Data persistence
4. **Backend → Filesystem**: Logging

### 4.3 Data at Rest

- API tokens and credentials are encrypted at rest
- Database may be SQLite (file-based) or PostgreSQL
- Users are responsible for database-level encryption if required

### 4.4 Data in Transit

- All external API calls use HTTPS (TLS 1.2+)
- Local UI-to-backend communication uses HTTPS by default
- Certificate can be user-provided or self-signed (for development)

---

## 5. Risk Assessment

### 5.1 Risk Assessment Summary

A full risk assessment is maintained separately. Summary of key risk areas:

| Risk Area | Severity | Likelihood | Mitigation |
|-----------|----------|------------|------------|
| Credential exposure | High | Low | Encryption at rest, secure configuration |
| Unauthorized access | High | Low | HTTPS, user-managed access control |
| Dependency vulnerabilities | Medium | Medium | Dependabot, regular updates |
| Data integrity | Medium | Low | Database transactions, input validation |
| Denial of service | Low | Low | Self-hosted (user manages infrastructure) |

### 5.2 Threat Model

Primary threat vectors considered:
- Compromised work tracking system credentials
- Unauthorized access to self-hosted instance
- Vulnerabilities in third-party dependencies
- Injection attacks via API inputs

### 5.3 Residual Risk

Lighthouse is self-hosted software. Users accept responsibility for:
- Network security and firewall configuration
- Access control and authentication
- Infrastructure security
- Backup and recovery

---

## 6. Security Controls

### 6.1 Security-by-Design Principles

| Principle | Implementation |
|-----------|----------------|
| Least privilege | Minimal permissions required for operation |
| Defense in depth | Multiple layers of validation and protection |
| Secure defaults | HTTPS enabled, minimal default exposure |
| Fail secure | Errors do not expose sensitive information |

### 6.2 Implemented Controls

| Control | Description | Status |
|---------|-------------|--------|
| Token encryption | API tokens encrypted at rest | ✅ Implemented |
| HTTPS | TLS encryption for all connections | ✅ Implemented |
| Input validation | API input validation and sanitization | ✅ Implemented |
| Dependency scanning | Automated vulnerability scanning | ✅ Implemented |
| Code signing | Signed executables (Windows, macOS) | ✅ Implemented |
| Container signing | Cosign signatures on Docker images | ✅ Implemented |
| Code analysis | SonarCloud static analysis | ✅ Implemented |

### 6.3 Logging

Security-relevant events are logged:
- Application startup/shutdown
- Configuration changes
- Error conditions
- API access (configurable log level)

Logging is minimal and avoids personal data. Users control log retention.

---

## 7. Software Bill of Materials

### 7.1 SBOM Availability

| Format | Location | Update Frequency |
|--------|----------|------------------|
| SPDX | Attached to GitHub Releases | Each release |

### 7.2 SBOM Scope

The SBOM includes:
- All backend NuGet dependencies
- All frontend npm dependencies
- Runtime components

### 7.3 SBOM Generation

SBOM is generated automatically during the CI/CD release process.

*Note: SBOM generation is being implemented as part of CRA conformance work.*

---

## 8. Update Mechanism

### 8.1 Update Notification

Lighthouse includes an update check mechanism:
- Backend queries GitHub Releases API for new versions
- UI displays available updates to users
- Users manually download and install updates

### 8.2 Update Process

| Deployment | Update Method |
|------------|---------------|
| Desktop (Windows/Linux/macOS) | Download new release, replace files |
| Docker | Pull new image tag |

### 8.3 Security Updates

See [Security Update Policy](./security-update-policy.md) for:
- Vulnerability response timelines
- Severity classification
- Communication channels

---

## 9. Testing and Verification

### 9.1 Automated Testing

| Test Type | Tool/Framework | Coverage |
|-----------|----------------|----------|
| Unit tests (Backend) | xUnit | Core logic |
| Unit tests (Frontend) | Vitest | Component logic |
| Integration tests | xUnit | API endpoints |
| E2E tests | Playwright | User workflows |
| Static analysis | SonarCloud | Code quality, security |

### 9.2 Security Testing

| Test Type | Tool | Frequency |
|-----------|------|-----------|
| Dependency scanning | Dependabot | Continuous |
| SAST | SonarCloud | Each PR/commit |
| Code review | Manual | Each PR |

### 9.3 CI/CD Pipeline

All changes go through:
1. Automated build
2. Unit and integration tests
3. Static analysis
4. Code review
5. Merge to main triggers release workflow

---

## 10. Vulnerability Handling

### 10.1 Reporting Channel

**Email**: security@letpeople.work

### 10.2 PSIRT Contacts

| Role | Contact |
|------|---------|
| PSIRT Contact (Main) | Benjamin Huser-Berta (benjamin@letpeople.work) |
| PSIRT Contact (Backup) | Peter Zylka-Greger (peter@letpeople.work) |

### 10.3 Process Documentation

- [SECURITY.md](https://github.com/LetPeopleWork/Lighthouse/blob/main/SECURITY.md) — Public reporting instructions
- [PSIRT Process](./psirt-process.md) — Internal handling process
- [Security Update Policy](./security-update-policy.md) — Response timelines

---

## 11. Conformity Assessment

### 11.1 Assessment Method

Conformity assessment performed using:
- **Module A: Internal production control** (self-assessment)

As permitted for standard products with digital elements under CRA.

### 11.2 Self-Assessment

See [CRA Self-Assessment](./cra-self-assessment.md) for detailed checklist.

### 11.3 Declaration of Conformity

See [Declaration of Conformity](./declaration-of-conformity.md) for the EU DoC.

---

## 12. References

### 12.1 Regulatory

- Regulation (EU) 2024/2847 — Cyber Resilience Act
- Regulation (EU) 2019/1020 — Market Surveillance Regulation

### 12.2 Product Documentation

- [Lighthouse Documentation](https://docs.lighthouse.letpeople.work)
- [GitHub Repository](https://github.com/LetPeopleWork/Lighthouse)
- [Release Notes](https://docs.lighthouse.letpeople.work/releasenotes/releasenotes.html)

### 12.3 Compliance Documents

- [Roles and Contacts](./roles-and-contacts.md)
- [Distribution and Versioning](./distribution-and-versioning.md)
- [Security Update Policy](./security-update-policy.md)
- [PSIRT Process](./psirt-process.md)
- [CRA Self-Assessment](./cra-self-assessment.md)
- [Declaration of Conformity](./declaration-of-conformity.md)

---

**Document Retention**: This technical file and supporting evidence shall be retained for 10 years after the last version of Lighthouse is placed on the market or put into service.

**Document Version**: 1.0  
**Last Updated**: 2025-12-30  
**Next Review**: 2026-12-30
