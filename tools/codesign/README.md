# Code Signing Runner

Docker-based ephemeral GitHub Actions runner for signing Windows binaries with a YubiKey hardware token.

## Overview

This container:
1. Registers as an ephemeral GitHub Actions self-hosted runner
2. Processes one code signing job
3. Exits automatically (ephemeral mode)

The runner uses GitHub App authentication for secure, scoped access.

## Required Secrets

Configure these in GitHub repository settings:

| Secret | Description |
|--------|-------------|
| `YUBIKEY_PIN` | PIN for the YubiKey PKCS#11 token |

## GitHub App Setup

1. **Create GitHub App** at `https://github.com/settings/apps/new`:
   - Name: `Lighthouse CodeSign Runner`
   - Homepage URL: Your repo URL
   - Uncheck "Webhook active"
   - Permissions:
     - Repository: `Actions: Read and write`
     - Repository: `Administration: Read-only` (for runner registration)
   - Where can this app be installed: `Only on this account`

2. **Generate private key**: On the App settings page, generate and download a private key (`.pem` file)

3. **Install the App**: Install it on the `LetPeopleWork/Lighthouse` repository

4. **Note the IDs**:
   - App ID: Shown on App settings page
   - Installation ID: From URL after installing (e.g., `https://github.com/settings/installations/12345678`)

## Local Setup

### Option 1: `.secure_env` directory (recommended)

Create files in `tools/codesign/.secure_env/`:

```
.secure_env/
├── GITHUB_APP_ID              # Just the App ID number
├── GITHUB_APP_INSTALLATION_ID # Just the Installation ID number
├── GITHUB_APP_PRIVATE_KEY     # Full PEM file contents
└── YUBIKEY_PIN                # Your YubiKey PIN
```

**Note:** The `.secure_env/` directory is gitignored.

### Option 2: Environment variables

```bash
export GITHUB_APP_ID="123456"
export GITHUB_APP_INSTALLATION_ID="12345678"
export GITHUB_APP_PRIVATE_KEY="$(cat /path/to/private-key.pem)"
export YUBIKEY_PIN="your-pin"
```

## Running Locally

With YubiKey plugged in:

```bash
# Pull from registry and run
./start-runner.sh

# Or build locally first
./start-runner.sh --build
```

The container will:
1. Start the PC/SC daemon for YubiKey access
2. Authenticate with GitHub using the App credentials
3. Register as an ephemeral runner
4. Wait for a job, execute it, then exit

## CI Flow

```
packageapp → verifysqlite/verifypostgres/docker
                          ↓
                   codesign-windows (self-hosted, requires approval)
                          ↓
                       release
```

The `codesign-windows` job:
- Runs on `[self-hosted, codesign]` labels
- Requires manual approval via `CodeSign` environment
- Has 5-minute timeout (start runner before approving)
- Downloads unsigned artifact, signs `Lighthouse.exe`, re-uploads

## Files

| File | Purpose |
|------|---------|
| `Dockerfile` | Container image with GitHub runner + signing tools |
| `entrypoint.sh` | Registers runner and starts it |
| `start-runner.sh` | Local convenience script |
| `sign.sh` | Signs Windows PE files with YubiKey |
| `verify.sh` | Verifies Authenticode signatures |
