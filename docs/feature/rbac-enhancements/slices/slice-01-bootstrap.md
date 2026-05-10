# Slice 01: Bootstrap and Initial Admin Setup

**Feature**: rbac-enhancements
**Delivery Sequence**: 1 of 4 (Walking Skeleton — deliver first)
**Estimated Effort**: 2-3 days

## Learning Hypothesis

A first-time System Admin can fully initialise RBAC (bootstrap + SSO group) from within the Lighthouse app in under 5 minutes, without editing server configuration files or escalating to IT.

## Stories in This Slice

| Story | Priority | What it delivers |
|---|---|---|
| US-01 | P1 (WS) | Bootstrap as first System Admin; assign SSO group; license banner |
| US-02 | P1 (WS) | Emergency admin displayed distinctly; non-revocable via UI |
| US-03 | P2 | Status chips replaced by collapsed diagnostic panel |
| US-11 | P1 (WS) | E2E scenarios 1-4: bootstrap + viewer restriction + admin flow + emergency fallback |

## Design Decisions Resolved in This Slice

- Q1: License upload is pre-auth; informational banner guides bootstrap
- Q2: Emergency admin shows "Emergency Admin" with lock icon, no Revoke button
- Q3: Onboarding stepper shown only to users with canCreateTeam/canCreatePortfolio
- Q5: Status chips removed; replaced by collapsed diagnostic panel

## Acceptance Gate

This slice is complete when:
1. A test user can log in to a fresh RBAC-enabled instance, bootstrap as System Admin, and assign an SSO group — without any config file changes
2. The emergency admin is visually distinct and cannot be accidentally revoked via UI
3. E2E scenarios 1-4 pass in CI

## Dependencies

- Backend: `POST /bootstrap/system-admin` endpoint — already implemented
- Backend: `RbacUser.isEmergencyAdmin` field — **new field required** (US-02 dependency)
- Test environment: emergency admin configured in `appsettings.json`
- Test environment: at least 2 test user accounts (test user + new sys admin)
