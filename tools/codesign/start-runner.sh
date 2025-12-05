#!/usr/bin/env bash
set -euo pipefail

# start-runner.sh
# Local convenience script to build and run the codesign runner container
# Reads GitHub App credentials and YubiKey PIN from environment variables

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
IMAGE_NAME="ghcr.io/letpeoplework/lighthouse-codesign-runner:latest"
LOCAL_IMAGE_NAME="lighthouse-codesign-runner:local"

usage() {
  cat <<'USG'
Usage: start-runner.sh [OPTIONS]

Starts the ephemeral GitHub Actions codesign runner container.

Options:
  --build         Build the image locally instead of pulling from registry
  --local         Use local image (lighthouse-codesign-runner:local)
  -h, --help      Show this help message

Required environment variables:
  GITHUB_APP_ID                 GitHub App ID
  GITHUB_APP_INSTALLATION_ID    GitHub App Installation ID
  GITHUB_APP_PRIVATE_KEY        GitHub App private key (PEM format)
  YUBIKEY_PIN                   YubiKey PIN for code signing

Optional environment variables:
  GITHUB_OWNER                  Repository owner (default: LetPeopleWork)
  GITHUB_REPO                   Repository name (default: Lighthouse)
  RUNNER_NAME                   Runner name (default: codesign-runner)

Example:
  export GITHUB_APP_ID="123456"
  export GITHUB_APP_INSTALLATION_ID="12345678"
  export GITHUB_APP_PRIVATE_KEY="$(cat /path/to/private-key.pem)"
  export YUBIKEY_PIN="123456"
  ./start-runner.sh
USG
}

BUILD_LOCAL=false
USE_LOCAL=false

while [[ $# -gt 0 ]]; do
  case $1 in
    --build)
      BUILD_LOCAL=true
      shift
      ;;
    --local)
      USE_LOCAL=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage
      exit 1
      ;;
  esac
done

# Try to load from .secure_env directory if env vars not set
SECURE_ENV_DIR="$SCRIPT_DIR/.secure_env"
if [ -d "$SECURE_ENV_DIR" ]; then
  [ -z "${GITHUB_APP_ID:-}" ] && [ -f "$SECURE_ENV_DIR/GITHUB_APP_ID" ] && \
    GITHUB_APP_ID=$(cat "$SECURE_ENV_DIR/GITHUB_APP_ID")
  [ -z "${GITHUB_APP_INSTALLATION_ID:-}" ] && [ -f "$SECURE_ENV_DIR/GITHUB_APP_INSTALLATION_ID" ] && \
    GITHUB_APP_INSTALLATION_ID=$(cat "$SECURE_ENV_DIR/GITHUB_APP_INSTALLATION_ID")
  [ -z "${GITHUB_APP_PRIVATE_KEY:-}" ] && [ -f "$SECURE_ENV_DIR/GITHUB_APP_PRIVATE_KEY" ] && \
    GITHUB_APP_PRIVATE_KEY=$(cat "$SECURE_ENV_DIR/GITHUB_APP_PRIVATE_KEY")
  [ -z "${YUBIKEY_PIN:-}" ] && [ -f "$SECURE_ENV_DIR/YUBIKEY_PIN" ] && \
    YUBIKEY_PIN=$(cat "$SECURE_ENV_DIR/YUBIKEY_PIN")
fi

# Validate required environment variables
missing_vars=()
[ -z "${GITHUB_APP_ID:-}" ] && missing_vars+=("GITHUB_APP_ID")
[ -z "${GITHUB_APP_INSTALLATION_ID:-}" ] && missing_vars+=("GITHUB_APP_INSTALLATION_ID")
[ -z "${GITHUB_APP_PRIVATE_KEY:-}" ] && missing_vars+=("GITHUB_APP_PRIVATE_KEY")
[ -z "${YUBIKEY_PIN:-}" ] && missing_vars+=("YUBIKEY_PIN")

if [ ${#missing_vars[@]} -gt 0 ]; then
  echo "Error: Missing required environment variables: ${missing_vars[*]}" >&2
  echo "You can set them in environment or place files in $SECURE_ENV_DIR/" >&2
  echo "Run '$0 --help' for usage information." >&2
  exit 1
fi

# Determine which image to use
if [ "$USE_LOCAL" = true ]; then
  IMAGE="$LOCAL_IMAGE_NAME"
elif [ "$BUILD_LOCAL" = true ]; then
  IMAGE="$LOCAL_IMAGE_NAME"
  echo "Building Docker image locally..."
  docker build -t "$IMAGE" "$SCRIPT_DIR"
else
  IMAGE="$IMAGE_NAME"
  echo "Pulling Docker image from registry..."
  docker pull "$IMAGE" || {
    echo "Failed to pull image. Try running with --build to build locally." >&2
    exit 1
  }
fi

echo "Starting codesign runner container..."
echo "  Image: $IMAGE"
echo "  Runner will process one job and exit (ephemeral mode)"
echo ""

# Create temp env file for multi-line private key (Docker -e doesn't handle newlines well)
ENV_FILE=$(mktemp)
chmod 600 "$ENV_FILE"
cat > "$ENV_FILE" <<EOF
GITHUB_APP_ID=$GITHUB_APP_ID
GITHUB_APP_INSTALLATION_ID=$GITHUB_APP_INSTALLATION_ID
YUBIKEY_PIN=$YUBIKEY_PIN
GITHUB_OWNER=${GITHUB_OWNER:-LetPeopleWork}
GITHUB_REPO=${GITHUB_REPO:-Lighthouse}
RUNNER_NAME=${RUNNER_NAME:-codesign-runner}
EOF

# Clean up env file on exit
trap 'rm -f "$ENV_FILE"' EXIT

# Run container with:
# - --rm: Remove container after exit
# - --privileged: Required for USB device access (YubiKey)
# - -v /dev/bus/usb: Mount USB devices for YubiKey
# - --env-file: Simple env vars
# - Private key passed via stdin to avoid shell escaping issues
exec docker run --rm -i \
  --privileged \
  -v /dev/bus/usb:/dev/bus/usb \
  --env-file "$ENV_FILE" \
  -e GITHUB_APP_PRIVATE_KEY \
  "$IMAGE"
