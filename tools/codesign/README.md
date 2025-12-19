# Windows Code Signing Setup

This directory contains the setup script for configuring a self-hosted GitHub Actions runner on Windows for code signing the Lighthouse application.

## Overview

The `start-runner-windows.ps1` script automates the setup and configuration of a GitHub Actions self-hosted runner specifically for Windows code signing operations. This runner is used in the CI/CD pipeline to digitally sign Windows executables and DLLs.

## Prerequisites

### Required Software
- Windows 10/11 or Windows Server 2019+
- PowerShell 5.1 or later
- Internet connectivity to download the GitHub Actions runner

### Required Credentials
- **GitHub Personal Access Token (PAT)** with the following scopes:
  - `repo` (full control of private repositories)
  - `admin:org` or repository admin permissions (for self-hosted runners)

### Code Signing Certificate
The machine running this script must have:
- A valid code signing certificate installed in the Windows Certificate Store
- The certificate thumbprint available as an environment variable or GitHub secret
- `signtool.exe` available (typically installed with Windows SDK or Visual Studio)

## Quick Start

1. **Set the required environment variable:**
   ```powershell
   $env:GITHUB_PAT = "ghp_your_personal_access_token_here"
   ```

2. **Run the setup script:**
   ```powershell
   .\start-runner-windows.ps1
   ```

3. **Verify the runner is connected:**
   - Navigate to your GitHub repository
   - Go to Settings → Actions → Runners
   - You should see `codesign-runner` listed as online

## Configuration Options

The script accepts the following environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `GITHUB_PAT` | Personal Access Token (required) | *none* |
| `GITHUB_OWNER` | Repository owner/organization | `LetPeopleWork` |
| `GITHUB_REPO` | Repository name | `Lighthouse` |
| `RUNNER_NAME` | Name for the runner instance | `codesign-runner` |

### Example: Custom Configuration

```powershell
$env:GITHUB_PAT = "ghp_your_token"
$env:GITHUB_OWNER = "YourOrg"
$env:GITHUB_REPO = "YourRepo"
$env:RUNNER_NAME = "windows-signer-01"
.\start-runner-windows.ps1
```

## How It Works

1. **Runner Download**: Downloads the latest GitHub Actions runner for Windows if not already present
2. **Token Request**: Requests a registration token from GitHub API using the PAT
3. **Configuration**: Configures the runner with labels `windows` and `codesign`
4. **Start**: Launches the runner in interactive mode

## Runner Labels

The runner is configured with the following labels:
- `windows` - Indicates Windows platform
- `codesign` - Indicates code signing capability

These labels are used in the workflow file to target this specific runner:
```yaml
runs-on: [self-hosted, windows, codesign]
```

## Code Signing Environment

### Required GitHub Secrets

The following secrets must be configured in your repository for the code signing workflow to function:

- `CERT_THUMBPRINT` - The thumbprint of the code signing certificate

### Certificate Setup

1. Install your code signing certificate in the Windows Certificate Store:
   - Open Certificate Manager (`certmgr.msc`)
   - Import the certificate to "Personal/Certificates"
   - Note the certificate thumbprint

2. Set the thumbprint as a GitHub secret:
   - Repository Settings → Secrets and variables → Actions
   - Create secret: `CERT_THUMBPRINT`

## Troubleshooting

### Common Issues

#### Token Request Fails
```
Failed to get registration token.
```
**Solution**: Verify that:
- Your PAT has the correct scopes (`repo` and admin permissions)
- The repository path is correct (`GITHUB_OWNER/GITHUB_REPO`)
- The repository exists and you have access to it

#### Runner Configuration Fails
```
Failed to configure runner
```
**Solution**: 
- Check if another runner with the same name already exists
- The script uses `--replace` flag to replace existing runners with the same name
- Ensure you have write permissions to the `actions-runner` directory

#### Code Signing Fails in Workflow
```
Signing failed: Certificate not found
```
**Solution**:
- Verify the certificate is installed in the machine's certificate store
- Check that `CERT_THUMBPRINT` secret matches the installed certificate
- Ensure `signtool.exe` is in the system PATH

### Viewing Runner Logs

Runner logs are stored in:
```
actions-runner\_diag\
```

For detailed troubleshooting, check:
- `Worker_*.log` - Runner execution logs
- `Runner_*.log` - Runner service logs

## Stopping the Runner

To stop the runner:
1. Press `Ctrl+C` in the PowerShell window
2. The runner will complete the current job before stopping

## Removing the Runner

To completely remove the runner:

```powershell
cd actions-runner
.\config.cmd remove --token YOUR_TOKEN
```

Or delete it from the GitHub UI:
- Repository Settings → Actions → Runners
- Click the runner name
- Click "Remove"

## Security Considerations

- Store the `GITHUB_PAT` securely - do not commit it to version control
- Regularly rotate your Personal Access Token
- Limit the PAT scope to only what's necessary
- The code signing certificate's private key must be protected
- Consider using a dedicated machine or VM for code signing
- Review the runner logs periodically for suspicious activity

## Integration with CI/CD

This runner is used by the `codesign-windows` job in `.github/workflows/ci.yml`:

```yaml
codesign-windows:
  name: Sign Windows Binaries
  runs-on: [self-hosted, windows, codesign]
  environment:
    name: CodeSign
  steps:
    - name: Sign EXE and DLL files
      shell: pwsh
      run: |
        $thumbprint = $env:CERT_THUMBPRINT
        # ... signing logic
```

## Support

For issues related to:
- **Runner setup**: Check GitHub Actions documentation
- **Code signing**: Verify certificate installation and signtool availability
- **Lighthouse CI/CD**: Open an issue in the Lighthouse repository

## Additional Resources

- [GitHub Actions Self-Hosted Runners Documentation](https://docs.github.com/en/actions/hosting-your-own-runners)
- [Windows Code Signing Guide](https://docs.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools)
- [SignTool Documentation](https://docs.microsoft.com/en-us/windows/win32/seccrypto/signtool)