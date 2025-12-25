# Lighthouse Code Signing Setup

This directory contains the automation scripts for configuring self-hosted GitHub Actions runners for code signing. It supports both Windows-native signing and Linux-based signing using YubiKey hardware.

## Overview

Lighthouse uses a cross-platform signing strategy to ensure binaries are trusted on Windows regardless of the build environment:
- **Windows Runners**: Use signtool.exe with certificates stored in the Windows Certificate Store.
- **Linux Runners (CachyOS/Arch)**: Use osslsigncode and YubiKey (FIPS/PIV) hardware for OV/EV code signing.

---

## Prerequisites

### Global
- **GitHub Personal Access Token (PAT)**: Requires repo and admin:org (or repository admin) scopes.
- **Self-Hosted Environment**: A machine dedicated to signing with the necessary hardware/software.

### Windows Requirements
- **Windows SDK**: signtool.exe must be accessible in the system PATH.
- **Certificate**: The code signing certificate must be imported into the "Personal" certificate store of the user running the script.

### Linux Requirements (CachyOS/Arch)
- **Packages**: opensc, libp11, pcsclite, osslsigncode.
- **Services**: pcscd.service must be enabled and running.
- **Hardware**: YubiKey plugged in with a Code Signing certificate (typically Slot 9a or 9c).
- **Permissions**: The user must have permissions to access the USB smart card (check security group).

---

## Quick Start

### 1. Set Environment Variables

Before running the registration scripts, set your PAT in your current shell session.

**PowerShell (Windows):**
```powershell
$env:GITHUB_PAT = "ghp_your_token_here"
```

**Fish (Linux):**
```fish
set -gx GITHUB_PAT "ghp_your_token_here"
```

### 2. Execute Setup Script

**On Windows:**
```powershell
.\start-runner-windows.ps1
```

**On Linux:**
```bash
pwsh ./start-runner-linux.ps1
```
*Note: The Linux script includes a "Pre-Flight" check to verify that the YubiKey is plugged in and the Code Signing key (ID 01) is accessible via PKCS#11 before it attempts to register the runner.*

---

## Configuration Options

The scripts accept the following environment variables to customize the runner registration:

| Variable | Description | Default |
|----------|-------------|---------|
| GITHUB_PAT | Personal Access Token (required) | none |
| GITHUB_OWNER | Repository owner/organization | LetPeopleWork |
| GITHUB_REPO | Repository name | Lighthouse |
| RUNNER_NAME | Name for the runner instance | codesign-runner-[os] |

---

## GitHub Actions Integration

The CI/CD pipeline targets these runners using specific labels.

### Runner Labels
- **Windows**: [self-hosted, windows, codesign]
- **Linux**: [self-hosted, linux, codesign]

### Required GitHub Secrets
- CERT_THUMBPRINT: (Windows) The SHA1 thumbprint of the installed certificate.
- YUBI_PIN: (Linux) The User PIN for the YubiKey.

### Signing Commands Used

The following commands are executed by the GitHub Action workflow depending on the runner OS.

**Windows Runner (signtool.exe):**
```powershell
signtool.exe sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /sha1 $thumbprint /d "Lighthouse $version" /du "https://letpeople.work/lighthouse" $file
```

**Linux Runner (osslsigncode):**
```bash
osslsigncode sign \
    -pkcs11engine /usr/lib/engines-3/pkcs11.so \
    -pkcs11module /usr/lib/opensc-pkcs11.so \
    -key "pkcs11:id=%01;type=private" \
    -pkcs11cert "pkcs11:id=%01;type=cert" \
    -pass "$YUBI_PIN" \
    -n "Lighthouse $version" \
    -i "https://letpeople.work/lighthouse" \
    -t http://timestamp.digicert.com \
    -in $file -out $file.signed
```

---

## Troubleshooting

### Linux / YubiKey
- **pcscd errors**: Ensure the smart card daemon is running: sudo systemctl enable --now pcscd.
- **Key not found**: Verify key visibility with:
  pkcs11-tool --module /usr/lib/opensc-pkcs11.so --list-objects --login
- **Permissions**: If the runner cannot see the YubiKey, ensure your user is in the security group on Arch/CachyOS.

### Windows
- **Thumbprint mismatch**: Double-check the thumbprint in certmgr.msc. Ensure there are no hidden spaces or non-alphanumeric characters when pasting into GitHub Secrets.
- **Signtool path**: If the command is not found, ensure the Windows SDK bin folder is added to the System Environment Variables.

---

## Security Considerations

- **FIPS Compliance**: On Linux, the private key is marked as non-extractable on the YubiKey. osslsigncode communicates with the hardware via PKCS#11, ensuring the key never leaves the hardware.
- **PAT Security**: The Personal Access Token is used only for the initial registration/token request. It is not stored in plain text by the runner after configuration.
- **Physical Access**: Since the Linux runner requires a physical YubiKey, the host machine should be kept in a physically secure location to prevent unauthorized use of the signing hardware.

---

## Support & Resources
- [GitHub Actions Self-Hosted Runners Guide](https://docs.github.com/en/actions/hosting-your-own-runners)
- [Yubico PIV Tool Documentation](https://developers.yubico.com/yubico-piv-tool/)
- [osslsigncode GitHub Repository](https://github.com/mtrojnar/osslsigncode)