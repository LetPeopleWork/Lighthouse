---
title: PSIRT Process
layout: home
parent: Compliance
nav_order: 4
---

# PSIRT Process

This document describes the internal Product Security Incident Response Team (PSIRT) process for handling security vulnerabilities in Lighthouse.

## Overview

The PSIRT process covers the complete lifecycle of a security vulnerability:

1. **Intake** — Receiving and logging the report
2. **Triage** — Validating, reproducing, and classifying the issue
3. **Fix** — Developing and testing a remediation
4. **Disclosure** — Communicating the fix to users
5. **Postmortem** — Learning from the incident

## PSIRT Team

| Role | Assignee | Contact |
|------|----------|---------|
| **PSIRT Contact (Main)** | Benjamin Huser-Berta | benjamin@letpeople.work |
| **PSIRT Contact (Backup)** | Peter Zylka-Greger | peter@letpeople.work |

## Phase 1: Intake

### Intake Channels

| Channel | Destination |
|---------|-------------|
| **Primary**: security@letpeople.work | Shared inbox monitored by PSIRT team |
| **Alternative**: GitHub Security Advisories | Private vulnerability reporting (if enabled) |

### Intake Checklist

When a report is received:

- [ ] Log the report in the vulnerability tracker (private)
- [ ] Assign a unique tracking ID
- [ ] Send acknowledgement to reporter within **5 business days**
- [ ] Include:
  - Tracking ID
  - Expected timeline for triage
  - Request for any additional information needed

### Intake Template (Acknowledgement)

```
Subject: [Lighthouse Security] Report Received - Tracking ID: SEC-YYYY-NNN

Thank you for reporting a potential security vulnerability in Lighthouse.

We have received your report and assigned tracking ID: SEC-YYYY-NNN

Next steps:
- We will triage this report within 10 business days
- We will contact you if we need additional information
- We will provide a status update once triage is complete

Please reference the tracking ID in any follow-up communications.

Best regards,
Lighthouse PSIRT
```

## Phase 2: Triage

### Triage Timeline

Target: **Within 10 business days** of intake

### Triage Steps

1. **Validate**: Confirm the report describes a real security issue
2. **Reproduce**: Attempt to reproduce the vulnerability
3. **Classify**: Assign severity using CVSS or simplified classification
4. **Scope**: Identify affected versions and components
5. **Decide**: Determine next action

### Severity Classification

| Severity | CVSS | Typical Examples |
|----------|------|------------------|
| **Critical** | 9.0–10.0 | RCE, complete auth bypass, data breach |
| **High** | 7.0–8.9 | Auth bypass (limited), sensitive data exposure |
| **Medium** | 4.0–6.9 | CSRF, limited info disclosure, requires auth |
| **Low** | 0.1–3.9 | Minor info leak, requires unlikely conditions |

### Actively Exploited Flag

If there is credible evidence of active exploitation:
- Set **Actively Exploited = True**
- Escalate immediately regardless of CVSS
- Consider emergency mitigation / communication

### Triage Decisions

| Decision | When | Action |
|----------|------|--------|
| **Fix** | Valid vulnerability | Proceed to Phase 3 |
| **Defer** | Low severity, high effort | Document rationale, schedule for later |
| **Dispute** | Not a vulnerability | Document rationale, notify reporter |
| **Duplicate** | Already reported | Link to existing issue, notify reporter |
| **Out of Scope** | Third-party, not our code | Redirect to appropriate party |

### Triage Communication

Send triage outcome to reporter:
- Severity classification
- Expected fix timeline (if applicable)
- Any questions or requests for clarification

## Phase 3: Fix

### Fix Timeline Targets

| Severity | Target | Notes |
|----------|--------|-------|
| **Actively Exploited** | 30 calendar days | May publish interim mitigation within 10 business days |
| **Critical** | 60 calendar days | Priority scheduling |
| **High** | 90 calendar days | Standard priority |
| **Medium** | 180 calendar days | May bundle with regular release |
| **Low** | As time permits | Address opportunistically |

### Fix Process

1. **Branch**: Create a private branch for the fix (if sensitive)
2. **Develop**: Implement the remediation
3. **Test**: Verify the fix resolves the issue without regression
4. **Review**: Code review with security focus
5. **Prepare**: Draft release notes and advisory content
6. **Coordinate**: Align disclosure timing with reporter

### Emergency Mitigation

If a full fix will take longer than acceptable:
- Document workaround / mitigation steps
- Communicate to users via release notes or advisory
- Continue work on full fix

## Phase 4: Disclosure

### Disclosure Channels

| Channel | Use Case |
|---------|----------|
| **Release Notes** | All security fixes |
| **GitHub Security Advisory** | Significant vulnerabilities (High/Critical) |
| **CVE** | Optional; request if warranted and resources allow |

### Advisory Content

Each advisory should include:
- Affected versions
- Fixed version
- Description of the vulnerability (without exploit details)
- Severity rating
- Mitigation steps (if applicable)
- Credit to reporter (if desired)

### Coordinated Disclosure

- Default embargo: up to **90 days** from report
- Negotiate with reporter if more time needed
- Release fix and advisory simultaneously
- Notify reporter before public disclosure

## Phase 5: Postmortem

### When to Conduct

Conduct a postmortem for:
- Critical or High severity vulnerabilities
- Actively exploited vulnerabilities
- Vulnerabilities with significant user impact
- Cases where the process broke down

### Postmortem Template

```markdown
## Security Postmortem: SEC-YYYY-NNN

### Summary
[Brief description of the vulnerability]

### Timeline
- Date reported:
- Date acknowledged:
- Date triaged:
- Date fixed:
- Date disclosed:

### Root Cause
[How did this vulnerability come to exist?]

### Impact
[What was the actual or potential impact?]

### Response Evaluation
- What went well?
- What could be improved?

### Action Items
- [ ] Action item 1
- [ ] Action item 2

### Lessons Learned
[Key takeaways for future prevention]
```

## Vulnerability Tracker

Maintain a private vulnerability tracker with:
- Tracking ID (SEC-YYYY-NNN format)
- Report date
- Reporter contact
- Status (Intake / Triage / Fix / Disclosed / Closed)
- Severity
- Affected component(s)
- Fix version
- Disclosure date
- Notes

**Location**: Private (not in public repository)

## Annual Review

Review this process annually or after significant incidents:
- Update timelines if capacity changes
- Incorporate lessons learned
- Align with regulatory changes (e.g., CRA reporting obligations starting Sept 2026)

---

**Document Version**: 1.0  
**Last Updated**: 2025-12-30  
**Next Review**: 2026-12-30
