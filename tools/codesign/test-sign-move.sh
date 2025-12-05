#!/usr/bin/env bash
set -euo pipefail

echo "Running sign.sh overwrite behavior test"

tmpdir=$(mktemp -d)
trap 'rm -rf "$tmpdir"' EXIT

pushd "$tmpdir" >/dev/null

# create a fake osslsigncode that writes its -out argument to simulate signing
cat > osslsigncode <<'SH'
#!/usr/bin/env bash
# simple mock: locate -out <file> and write 'signed' to it
out=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    -out)
      shift
      out="$1"
      ;;
    *)
      shift
      ;;
  esac
done
if [ -z "$out" ]; then
  echo "no out provided" >&2
  exit 2
fi
printf 'signed' > "$out"
exit 0
SH
chmod +x osslsigncode

# ensure sign.sh uses our mock
export PATH="$tmpdir:$PATH"

# prepare an input file to sign
printf 'original' > app.exe

SCRIPT_DIR="$(cd "$(dirname "$0")" >/dev/null && pwd)"
# run sign.sh (it should call our mock osslsigncode and then move the -signed file back to app.exe)
"$SCRIPT_DIR/sign.sh" . 'app.exe'

content=$(cat app.exe)
if [ "$content" = "signed" ]; then
  echo "TEST PASSED: input file was replaced with signed output"
  exit 0
else
  echo "TEST FAILED: input file content is: $content" >&2
  exit 1
fi

popd >/dev/null
