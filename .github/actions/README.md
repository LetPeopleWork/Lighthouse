# Composite Actions (internal)

This directory contains minimal composite actions used across Lighthouse CI workflows:

- `build-frontend` - builds frontend using Node 20 + pnpm 9 and optional SonarCloud.
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

Pinning external action SHAs:
- Run `Scripts/pin-action-shas.sh` to get a mapping from action tags to commit SHAs.
- Run `Scripts/pin-action-shas.sh --apply` to replace all occurrences of `owner/repo@tag` with `owner/repo@<sha>` in `.github` files (this will create `.bak` backups of modified files).
- Review and commit the changes, then monitor CI.

Dependabot:
- `.github/dependabot.yml` now includes a `github-actions` entry that will propose updates for actions daily.
- The repo already has `dependabot-automerge.yaml` which enables auto-merge for Dependabot PRs when configured.
