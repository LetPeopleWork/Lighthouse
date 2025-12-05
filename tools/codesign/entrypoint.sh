#!/usr/bin/env bash
set -euo pipefail

# entrypoint.sh
# Starts pcscd for YubiKey access, registers as ephemeral GitHub Actions runner using GitHub App auth

# --- Configuration ---
GITHUB_OWNER=${GITHUB_OWNER:-LetPeopleWork}
GITHUB_REPO=${GITHUB_REPO:-Lighthouse}
RUNNER_NAME=${RUNNER_NAME:-codesign-runner}
RUNNER_LABELS=${RUNNER_LABELS:-self-hosted,linux,codesign}
RUNNER_WORKDIR=${RUNNER_WORKDIR:-/home/runner/actions-runner/_work}

# --- Validate required environment variables ---
missing_vars=()
[ -z "${GITHUB_APP_ID:-}" ] && missing_vars+=("GITHUB_APP_ID")
[ -z "${GITHUB_APP_INSTALLATION_ID:-}" ] && missing_vars+=("GITHUB_APP_INSTALLATION_ID")
[ -z "${GITHUB_APP_PRIVATE_KEY:-}" ] && missing_vars+=("GITHUB_APP_PRIVATE_KEY")

if [ ${#missing_vars[@]} -gt 0 ]; then
  echo "Error: Missing required environment variables: ${missing_vars[*]}" >&2
  echo "Required: GITHUB_APP_ID, GITHUB_APP_INSTALLATION_ID, GITHUB_APP_PRIVATE_KEY" >&2
  exit 1
fi

# --- Start PC/SC daemon for YubiKey access ---
echo "Starting pcscd daemon..."
sudo /usr/bin/pcscd --foreground &
PCSCD_PID=$!
sleep 2

# Check if pcscd is running
if ! kill -0 $PCSCD_PID 2>/dev/null; then
  echo "Warning: pcscd may not have started correctly" >&2
fi

# --- Generate GitHub App JWT ---
generate_jwt() {
  local app_id="$1"
  local private_key="$2"
  
  local now=$(date +%s)
  local iat=$((now - 60))
  local exp=$((now + 540))  # 9 minutes (max is 10)
  
  local header='{"alg":"RS256","typ":"JWT"}'
  local payload="{\"iat\":${iat},\"exp\":${exp},\"iss\":\"${app_id}\"}"
  
  local header_b64=$(echo -n "$header" | openssl base64 -e | tr -d '\n=' | tr '+/' '-_')
  local payload_b64=$(echo -n "$payload" | openssl base64 -e | tr -d '\n=' | tr '+/' '-_')
  
  local unsigned="${header_b64}.${payload_b64}"
  
  # Write private key to temp file, reconstructing PEM format if needed
  local key_file=$(mktemp)
  chmod 600 "$key_file"
  
  # Check if key has newlines or is all on one line
  if echo "$private_key" | grep -q "^-----BEGIN.*-----$"; then
    # Key has proper PEM format with newlines
    printf '%s\n' "$private_key" > "$key_file"
  else
    # Key is on single line (newlines were lost) - reconstruct PEM format
    # Extract header, body, and footer, then format properly
    local header=$(echo "$private_key" | grep -o '^-----BEGIN [^-]*-----')
    local footer=$(echo "$private_key" | grep -o '-----END [^-]*-----$')
    local body=$(echo "$private_key" | sed "s/^-----BEGIN [^-]*----- //" | sed "s/ -----END [^-]*-----$//" | tr ' ' '\n')
    
    {
      echo "$header"
      echo "$body"
      echo "$footer"
    } > "$key_file"
  fi
  
  # Sign and base64 encode in a single pipeline to avoid null byte issues
  local signature
  signature=$(echo -n "$unsigned" | openssl dgst -sha256 -sign "$key_file" -binary 2>/dev/null | openssl base64 -e | tr -d '\n=' | tr '+/' '-_')
  local sign_exit=$?
  
  # Clean up key file
  rm -f "$key_file"
  
  if [ $sign_exit -ne 0 ] || [ -z "$signature" ]; then
    echo "DEBUG: OpenSSL sign failed" >&2
    return 1
  fi
  
  echo "${unsigned}.${signature}"
}

echo "Generating GitHub App JWT..."
JWT=$(generate_jwt "$GITHUB_APP_ID" "$GITHUB_APP_PRIVATE_KEY")

if [ -z "$JWT" ]; then
  echo "Error: JWT generation failed" >&2
  exit 1
fi

# --- Get installation access token ---
echo "Fetching installation access token..."
INSTALL_RESPONSE=$(curl -s -X POST \
  -H "Authorization: Bearer $JWT" \
  -H "Accept: application/vnd.github+json" \
  "https://api.github.com/app/installations/${GITHUB_APP_INSTALLATION_ID}/access_tokens")

INSTALLATION_TOKEN=$(echo "$INSTALL_RESPONSE" | jq -r '.token')

if [ -z "$INSTALLATION_TOKEN" ] || [ "$INSTALLATION_TOKEN" = "null" ]; then
  echo "Error: Failed to get installation access token" >&2
  echo "Response: $INSTALL_RESPONSE" >&2
  exit 1
fi

# --- Get runner registration token ---
echo "Fetching runner registration token..."
REGISTRATION_TOKEN=$(curl -s -X POST \
  -H "Authorization: token $INSTALLATION_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  "https://api.github.com/repos/${GITHUB_OWNER}/${GITHUB_REPO}/actions/runners/registration-token" \
  | jq -r '.token')

if [ -z "$REGISTRATION_TOKEN" ] || [ "$REGISTRATION_TOKEN" = "null" ]; then
  echo "Error: Failed to get runner registration token" >&2
  exit 1
fi

# --- Configure the runner ---
echo "Configuring GitHub Actions runner..."
cd /home/runner/actions-runner

# Create work directory
mkdir -p "$RUNNER_WORKDIR"
chown -R runner:runner "$RUNNER_WORKDIR"

# Configure as ephemeral runner (one job, then exit)
sudo -u runner ./config.sh \
  --url "https://github.com/${GITHUB_OWNER}/${GITHUB_REPO}" \
  --token "$REGISTRATION_TOKEN" \
  --name "$RUNNER_NAME" \
  --labels "$RUNNER_LABELS" \
  --work "$RUNNER_WORKDIR" \
  --ephemeral \
  --unattended \
  --replace

# --- Run the runner (once) ---
echo "Starting GitHub Actions runner (ephemeral mode)..."
exec sudo -u runner ./run.sh --once
