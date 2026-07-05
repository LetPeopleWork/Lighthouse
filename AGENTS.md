# Agent Instructions

<!-- lean-ctx -->
## lean-ctx

lean-ctx is active — the MCP tools replace native equivalents.
Full rules: LEAN-CTX.md (open on demand — do not auto-load).
<!-- /lean-ctx -->

## OpenCode migration

This repo was migrated from Claude Code to OpenCode. Key changes:

- **Config**: `opencode.json` at repo root (replaces `.claude/settings.json` + `.claude/settings.local.json`)
- **Commands**: `.opencode/commands/<name>.md` — mirrored from `.claude/commands/`. The Claude Code originals are preserved for reference.
- **Instructions**: `opencode.json` references `AGENTS.md` and `CLAUDE.md` via the `instructions` field.
- **MCP lean-ctx**: configured in `opencode.json` (was in `.claude/settings.local.json` hooks)
- **Ledger hook**: `Scripts/ledger_check.sh` (Claude Code PreToolUse hook) no longer auto-runs on `git commit`. OpenCode doesn't have a hook system yet. Run `python3 Scripts/ledger_check.py` manually before committing if needed, or use `--no-verify` to bypass.
- **Permissions**: migrated from both `.claude/settings.json` and `.claude/settings.local.json` into `opencode.json` under `permission.bash`. The old `allowed-tools` per-command model doesn't exist in OpenCode — tools are controlled at config level.
- **Allowed tools renamed**: `AskUserQuestion` → `question` tool. The migrated command files use the correct OpenCode tool name.

Use `/help` to see available slash commands.
