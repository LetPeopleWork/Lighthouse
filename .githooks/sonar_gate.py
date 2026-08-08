#!/usr/bin/env python3
"""Decide whether a `sonar analyze` JSON result should block a push.

Reads the JSON emitted by `sonar analyze --format json` on stdin and exits:
  0  - nothing blocking (clean, or only infra-not-enabled warnings)
  1  - blocking findings (hardcoded secrets, or quality issues if available)

Secrets are scanned locally. Quality analysis runs server-side ("Agentic
Analysis"); when that feature is not available on the org's plan the CLI either
omits it or returns a 403 failure, which is an infrastructure gap rather than a
code defect, so it is warned, never blocked on.
"""
import json
import sys

INFRA_MARKERS = ("not activated", "not configured", "not enabled")

# Findings that are published test fixtures rather than credentials. Keyed by file and the
# CLI's masked rendering of the value, never by line number — lines move whenever the file is
# edited, and a stale entry would silence a real finding. A different secret in the same file
# still blocks.
ACKNOWLEDGED_SECRETS = {
    # The client secret of the example Keycloak realm in examples/keycloak/realm-export.json,
    # which ci_verifyauth.yml starts on localhost:38080 for the auth E2E jobs. The realm is
    # created from that export inside the CI container and grants nothing outside it.
    ("ci_verifyauth.yml", "vTa*****************************"),
}


def is_acknowledged(issue: dict) -> bool:
    return (issue.get("file"), issue.get("secret")) in ACKNOWLEDGED_SECRETS


def is_infra_failure(message: str) -> bool:
    lowered = message.lower()
    return any(marker in lowered for marker in INFRA_MARKERS)


def main() -> int:
    raw = sys.stdin.read().strip()
    if not raw:
        return 0

    try:
        result = json.loads(raw)
    except json.JSONDecodeError:
        print(f"sonar gate: could not parse analyze output:\n{raw}", file=sys.stderr)
        return 0

    reported_secrets = result.get("secrets", {}).get("issues", [])
    secret_issues = [issue for issue in reported_secrets if not is_acknowledged(issue)]
    acknowledged_count = len(reported_secrets) - len(secret_issues)
    agentic = result.get("agentic") or {}
    quality_issues = [
        issue
        for entry in agentic.get("files", [])
        for issue in entry.get("issues", [])
    ]
    failures = agentic.get("failures", [])

    blocking = bool(secret_issues) or bool(quality_issues)

    if acknowledged_count:
        print(f"  (skipped {acknowledged_count} acknowledged test fixture(s); see ACKNOWLEDGED_SECRETS)", file=sys.stderr)

    for issue in secret_issues:
        location = issue.get("file") or issue.get("path") or "?"
        print(f"  secret: {location} - {issue.get('ruleName', issue.get('rule', 'hardcoded secret'))}", file=sys.stderr)
    for issue in quality_issues:
        print(f"  issue: {issue.get('file', '?')}:{issue.get('line', '?')} - {issue.get('message', '')}", file=sys.stderr)

    for failure in failures:
        message = failure.get("message", "")
        if is_infra_failure(message):
            print(f"  (skipped quality analysis: {message.splitlines()[0]})", file=sys.stderr)
        else:
            print(f"  analyze failure: {failure.get('path', '?')} - {message}", file=sys.stderr)

    return 1 if blocking else 0


if __name__ == "__main__":
    sys.exit(main())
