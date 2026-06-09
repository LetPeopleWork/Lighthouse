#!/usr/bin/env python3
"""Ledger check (Claude Code PreToolUse hook on ``git commit``).

Reads the tool call from stdin. If it is a ``git commit``, it greps the staged
changes (the content about to be committed) against the machine-readable patterns
in ``docs/ci-learnings.md`` (between the ``LEDGER-CHECKS`` markers). Any match
exits 2, which blocks the commit and feeds the findings back to Claude so the
offending lines get fixed before the foot-gun ever enters git history.

Running at commit time keeps the batch small (only the current staged diff) and
needs no "since last push" bookkeeping.

Bypass: include ``--no-verify`` in the commit command (developer override).
"""

import json
import os
import re
import shlex
import subprocess
import sys

DELIM = " ::: "
START = "<!-- LEDGER-CHECKS:START -->"
END = "<!-- LEDGER-CHECKS:END -->"
SKIP_PATH_FRAGMENTS = (
    "docs/ci-learnings.md",
    "Scripts/ledger_check.py",
    "/node_modules/",
    "/bin/",
    "/obj/",
    "/dist/",
    "/wwwroot/",
    "TestResults/",
)


def run(args):
    result = subprocess.run(args, capture_output=True, text=True)
    return result.stdout if result.returncode == 0 else ""


def project_dir():
    env = os.environ.get("CLAUDE_PROJECT_DIR")
    if env:
        return env
    return run(["git", "rev-parse", "--show-toplevel"]).strip()


def load_checks(ledger_path):
    try:
        with open(ledger_path, encoding="utf-8") as handle:
            text = handle.read()
    except OSError:
        return []
    if START not in text or END not in text:
        return []
    block = text.split(START, 1)[1].split(END, 1)[0]
    checks = []
    for line in block.splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split(DELIM)
        if len(parts) != 4:
            continue
        rule, exts, pattern, hint = (part.strip() for part in parts)
        try:
            compiled = re.compile(pattern)
        except re.error:
            continue
        extensions = tuple(f".{ext.strip().lstrip('.')}" for ext in exts.split(","))
        checks.append((rule, extensions, compiled, hint))
    return checks


def commits_all_tracked(command):
    """True for ``git commit -a`` / ``--all`` (stages tracked files at commit time)."""
    try:
        tokens = shlex.split(command)
    except ValueError:
        tokens = command.split()
    for token in tokens:
        if token == "--all":
            return True
        if re.fullmatch(r"-[a-zA-Z]*a[a-zA-Z]*", token):
            return True
    return False


def staged_diff(project, command):
    if commits_all_tracked(command):
        # `-a` commits every tracked change, staged or not.
        return run(["git", "-C", project, "diff", "--no-color", "HEAD"])
    return run(["git", "-C", project, "diff", "--cached", "--no-color"])


def added_lines(diff):
    current_file = None
    new_lineno = 0
    for line in diff.splitlines():
        if line.startswith("+++ b/"):
            current_file = line[6:]
            continue
        if line.startswith("@@"):
            match = re.search(r"\+(\d+)", line)
            new_lineno = int(match.group(1)) if match else 0
            continue
        if line.startswith("+") and not line.startswith("+++"):
            yield current_file, new_lineno, line[1:]
            new_lineno += 1
        elif line.startswith(" "):
            new_lineno += 1
        # '-' lines do not advance the new-file line counter


def main():
    raw = sys.stdin.read()
    try:
        payload = json.loads(raw)
    except (ValueError, TypeError):
        sys.exit(0)

    command = (payload.get("tool_input") or {}).get("command", "")
    if "git commit" not in command:
        sys.exit(0)
    if "--no-verify" in command:
        sys.exit(0)

    project = project_dir()
    if not project:
        sys.exit(0)

    checks = load_checks(os.path.join(project, "docs", "ci-learnings.md"))
    if not checks:
        sys.exit(0)

    findings = []
    for path, lineno, content in added_lines(staged_diff(project, command)):
        if not path or any(fragment in path for fragment in SKIP_PATH_FRAGMENTS):
            continue
        for rule, extensions, compiled, hint in checks:
            if not path.endswith(extensions):
                continue
            if compiled.search(content):
                findings.append((path, lineno, rule, hint, content.strip()))

    if not findings:
        sys.exit(0)

    lines = [
        f"LEDGER CHECK blocked the commit: {len(findings)} known CI foot-gun(s) "
        "in the staged changes.",
        "These match recorded learnings in docs/ci-learnings.md and WILL fail the CI "
        "quality gate. Fix each line below, re-stage, and commit again (this hook "
        "re-runs and clears once they are gone). Override with --no-verify only if "
        "intentional.",
        "",
    ]
    for path, lineno, rule, hint, content in findings:
        lines.append(f"  {path}:{lineno}  [{rule}]")
        lines.append(f"      {content}")
        lines.append(f"      -> {hint}")
    print("\n".join(lines), file=sys.stderr)
    sys.exit(2)


if __name__ == "__main__":
    main()
