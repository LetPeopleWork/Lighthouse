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
# Run pcscd as root without polkit (polkit daemon doesn't run in containers)
# Kill any existing pcscd first
pkill pcscd 2>/dev/null || true
sleep 1

# Start pcscd with --disable-polkit to bypass polkit authorization
/usr/bin/pcscd --foreground --auto-exit --disable-polkit &
PCSCD_PID=$!
sleep 2

# Check if pcscd is running
if ! kill -0 $PCSCD_PID 2>/dev/null; then
  echo "Warning: pcscd may not have started correctly" >&2
fi

# Set environment for smart card access (belt and suspenders)
export PCSCLITE_NO_POLKIT=1
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
  
  # Handle different formats of private key:
  # 1. Proper PEM with newlines (from file)
  # 2. Single line with literal \n escape sequences (from some env var sources)
  # 3. Single line with spaces instead of newlines (Docker env)
  
  # Count actual newlines in the key
  local newline_count=$(printf '%s' "$private_key" | grep -c $'\n' || true)
  
  if [ "$newline_count" -gt 5 ]; then
    # Key has multiple newlines - likely proper PEM format
    printf '%s\n' "$private_key" > "$key_file"
  elif printf '%s' "$private_key" | grep -q '\\n'; then
    # Key has literal \n escape sequences - convert them to real newlines
    printf '%b\n' "$private_key" > "$key_file"
  else
    # Key is on single line (newlines were lost) - reconstruct PEM format
    # Format: "-----BEGIN RSA PRIVATE KEY----- base64... -----END RSA PRIVATE KEY-----"
    # Split on spaces and reconstruct with proper line breaks
    
    # Extract the key type from header
    local key_type=$(printf '%s' "$private_key" | sed -n 's/^-----BEGIN \([^-]*\)-----.*/\1/p')
    local pem_header="-----BEGIN ${key_type}-----"
    local pem_footer="-----END ${key_type}-----"
    
    # Remove header and footer, leaving just the base64 body parts separated by spaces
    local body=$(printf '%s' "$private_key" | \
      sed "s/^-----BEGIN ${key_type}----- //" | \
      sed "s/ -----END ${key_type}-----$//" | \
      tr ' ' '\n')
    
    {
      printf '%s\n' "$pem_header"
      printf '%s\n' "$body"
      printf '%s\n' "$pem_footer"
    } > "$key_file"
  fi
  
  # Verify the key file is valid before attempting to sign
  if ! openssl rsa -in "$key_file" -check -noout >/dev/null 2>&1; then
    echo "DEBUG: Invalid private key format" >&2
    echo "DEBUG: Key file starts with: $(head -c 50 "$key_file")" >&2
    rm -f "$key_file"
    return 1
  fi
  
  # Sign the JWT - use process substitution to capture errors properly
  local sig_file=$(mktemp)
  local sign_error
  sign_error=$(echo -n "$unsigned" | openssl dgst -sha256 -sign "$key_file" -out "$sig_file" 2>&1)
  local sign_exit=$?
  
  if [ $sign_exit -ne 0 ]; then
    echo "DEBUG: OpenSSL sign failed with exit code $sign_exit" >&2
    echo "DEBUG: Error: $sign_error" >&2
    rm -f "$key_file" "$sig_file"
    return 1
  fi
  
  # Base64 encode the signature
  local signature
  signature=$(openssl base64 -e -in "$sig_file" | tr -d '\n=' | tr '+/' '-_')
  
  # Clean up temp files
  rm -f "$key_file" "$sig_file"
  
  if [ -z "$signature" ]; then
    echo "DEBUG: Empty signature after base64 encoding" >&2
    return 1
  fi
  
  echo "${unsigned}.${signature}"
}

echo "Generating GitHub App JWT..."
JWT=$(generate_jwt "$GITHUB_APP_ID" "$GITHUB_APP_PRIVATE_KEY") || {
  echo "Error: JWT generation failed" >&2
  exit 1
}

if [ -z "$JWT" ]; then
  echo "Error: JWT generation returned empty string" >&2
  exit 1
fi

# --- Get installation access token ---
echo "Fetching installation access token..."
CURL_OUTPUT=$(mktemp)
CURL_STDERR=$(mktemp)
curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST \
  -H "Authorization: Bearer $JWT" \
  -H "Accept: application/vnd.github+json" \
  "https://api.github.com/app/installations/${GITHUB_APP_INSTALLATION_ID}/access_tokens" \
  > "$CURL_OUTPUT" 2> "$CURL_STDERR" || true

INSTALL_RESPONSE=$(cat "$CURL_OUTPUT")
rm -f "$CURL_OUTPUT" "$CURL_STDERR"

HTTP_STATUS=$(echo "$INSTALL_RESPONSE" | grep -o 'HTTP_STATUS:[0-9]*' | cut -d: -f2 || echo "000")
INSTALL_RESPONSE=$(echo "$INSTALL_RESPONSE" | sed '/HTTP_STATUS:/d')

INSTALLATION_TOKEN=$(echo "$INSTALL_RESPONSE" | jq -r '.token' 2>/dev/null || echo "null")

if [ -z "$INSTALLATION_TOKEN" ] || [ "$INSTALLATION_TOKEN" = "null" ]; then
  echo "Error: Failed to get installation access token (HTTP $HTTP_STATUS)" >&2
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

# --- Run the runner ---
echo "Starting GitHub Actions runner (ephemeral mode)..."
exec sudo -u runner ./run.sh
