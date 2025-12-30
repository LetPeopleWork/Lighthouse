---
title: Distribution and Versioning
layout: home
parent: Compliance
nav_order: 2
---

# Distribution and Versioning

This document describes how Lighthouse is distributed and versioned, as required for CRA compliance traceability.

## Distribution Channels

Lighthouse is distributed through the following channels:

| Channel | URL / Location | Artifact Types |
|---------|----------------|----------------|
| **GitHub Releases** | [github.com/LetPeopleWork/Lighthouse/releases](https://github.com/LetPeopleWork/Lighthouse/releases) | Windows zip, Linux zip, macOS dmg, macOS app zip |
| **GitHub Container Registry (GHCR)** | `ghcr.io/letpeoplework/lighthouse` | Docker images (linux/amd64, linux/arm64) |
| **Product Website** | [letpeople.work/lighthouse](https://letpeople.work/lighthouse) | Links to GitHub Releases |
| **Documentation** | [docs.lighthouse.letpeople.work](https://docs.lighthouse.letpeople.work) | Online documentation |

### No Additional Distribution Channels

There are no additional distribution channels such as:
- App stores or marketplaces
- Paid enterprise installers
- OEM bundles

## Product Editions

Lighthouse is distributed as a **single product version**. The Community and Premium editions are feature unlocks controlled by licensing—not separate binaries or builds.

| Edition | Licensing | Binary |
|---------|-----------|--------|
| Community | Free (feature-limited) | Same as Premium |
| Premium | License key required | Same as Community |

This means:
- **One set of release artifacts** per version
- **One SBOM** covers all editions
- **One security update** addresses all users

## Versioning Scheme

Lighthouse uses a **date-based versioning scheme** with the following format:

```
vYY.MM.DD.<build>
```

| Component | Description | Example |
|-----------|-------------|---------|
| `v` | Version prefix | `v` |
| `YY` | Two-digit year | `25` |
| `MM` | Two-digit month | `12` |
| `DD` | Two-digit day | `28` |
| `<build>` | Build number (time-based or sequential) | `1246` |

**Example**: `v25.12.28.1246` = December 28, 2025, build 1246

### Version Source of Truth

| Artifact | Version Source |
|----------|----------------|
| Git tag | Created automatically on `main` branch merge via `daily-version-action` |
| Desktop artifacts | Embedded from Git tag via MSBuild `/p:Version` |
| Docker image tag | Same as Git tag (without `v` prefix for file version) |
| API endpoint | Reports version via `/api/Version` |
| Release notes | References Git tag in documentation |

### Version Workflow

1. Merge to `main` triggers the CI workflow
2. `ci_tag.yml` creates a new version tag using `fregante/daily-version-action`
3. Build workflows use the tag as `fileversion` input
4. Release workflow publishes artifacts with the version tag
5. Docker images are tagged with both version and `latest`/`dev-latest`

## Release Artifacts

Each release includes the following artifacts:

| Artifact | Filename Pattern | Platforms |
|----------|------------------|-----------|
| Windows (zip) | `Lighthouse-win-x64.zip` | Windows x64 |
| Linux (zip) | `Lighthouse-linux-x64.zip` | Linux x64 |
| macOS (dmg) | `Lighthouse-osx.dmg` | macOS (Universal) |
| macOS (app zip) | `Lighthouse-osx.app.zip` | macOS (Universal) |
| Docker | `ghcr.io/letpeoplework/lighthouse:<version>` | linux/amd64, linux/arm64 |

### Code Signing

| Platform | Signing Status |
|----------|----------------|
| Windows | Digitally signed (LetPeopleWork GmbH certificate) |
| macOS | Signed and notarized by Apple |
| Docker | Signed with Cosign (keyless, GitHub OIDC) |
| Linux | Not signed (no standard mechanism) |

### CRA-Required Attachments (Planned)

Future releases will include:
- **SBOM**: Consolidated SPDX SBOM attached to GitHub Release
- **Compliance reference**: Link to CRA Technical File and Declaration of Conformity

---

**Document Version**: 1.0  
**Last Updated**: 2025-12-30  
**Next Review**: 2026-12-30
