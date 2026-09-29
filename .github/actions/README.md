# Composite Actions (internal)

This directory contains minimal composite actions used across Lighthouse CI workflows:

- `build-frontend` - builds frontend with the Node version from `.nvmrc` and the pnpm version from `packageManager`, with optional SonarCloud.
- `build-backend` - builds backend using .NET 10 and JDK 17; can publish self-contained outputs for common targets.
- `package-app` - builds frontend & backend and uploads artifacts used for releases.

Notes:
- `build-frontend` requires a Sonar token if `run-tests` is enabled. Pass it from workflows using the step-level env, e.g.:

```yaml
- name: Build Frontend (composite)
  uses: ./.github/actions/build-frontend
  with:
    run-tests: 'true'
  env:
    SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

Design principles:
- Minimal inputs: versions and paths are embedded in the composite actions to keep callers simple.
- Reuse: workflows call these composites to avoid duplicating build steps.

Pinning external actions:
- Every `uses:` is pinned as `@<sha> # <version>`, so Renovate can keep it current. The workflow-scripts check rejects any other form.

Dependency updates:
- Renovate (`renovate.json` at the repository root) proposes an update once a release is 7 days old.
- Updates merge themselves when the required checks pass.
- Three kinds wait for a maintainer instead: the Node and pnpm toolchain group, .NET runtime image majors, and chart value updates.
- Security fixes skip the 7-day wait.
- A vulnerable indirect dependency gets no bot update, even when GitHub's vulnerability alerts flag it. The pnpm audit on main catches it, and a maintainer adds an override.
