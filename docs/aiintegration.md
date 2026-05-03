---
title: AI and Automation
layout: home
nav_order: 40
---

Lighthouse can be used directly from scripts, terminals, coding agents, and MCP-enabled AI clients. The `lighthouse-clients` packages give you a supported way to automate Lighthouse data access and expose Lighthouse capabilities to LLMs without having to build custom glue code first.

{: .important}
This area is evolving quickly. If you find a client setup that works well, or a gap in the examples below, please let us know in our [Slack community](https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A).

- TOC
{:toc}

## Choose the Right Option

Lighthouse currently offers three main automation entry points:

| Option | Best for | Package |
| --- | --- | --- |
| CLI | Shell scripts, CI jobs, coding agents with terminal access, quick ad-hoc inspection | [@letpeoplework/lighthouse-cli](https://www.npmjs.com/package/@letpeoplework/lighthouse-cli) |
| MCP stdio | Local AI clients such as VS Code / GitHub Copilot, Claude Code, or other tools that can start a local process | [@letpeoplework/lighthouse-mcp-stdio](https://www.npmjs.com/package/@letpeoplework/lighthouse-mcp-stdio) |
| MCP HTTP | Shared or hosted AI setups, container deployments, remote development environments, and web-based clients that need a network endpoint | [@letpeoplework/lighthouse-mcp-http](https://www.npmjs.com/package/@letpeoplework/lighthouse-mcp-http) |

If your AI client can run terminal commands, the CLI is often the fastest way to automate Lighthouse. If your AI client supports MCP, use either the local stdio server or the shared HTTP server depending on your deployment model.

## Packages and Downloads

The current Lighthouse automation packages are published on npm:

- [@letpeoplework/lighthouse-cli](https://www.npmjs.com/package/@letpeoplework/lighthouse-cli)
- [@letpeoplework/lighthouse-mcp-stdio](https://www.npmjs.com/package/@letpeoplework/lighthouse-mcp-stdio)
- [@letpeoplework/lighthouse-mcp-http](https://www.npmjs.com/package/@letpeoplework/lighthouse-mcp-http)

Lighthouse also ships two ready-to-download assets from the latest `lighthouse-clients` release:

- [lighthouse-mcp-stdio.mcpb](https://github.com/LetPeopleWork/lighthouse-clients/releases/latest/download/lighthouse-mcp-stdio.mcpb) for one-click MCP bundle installation in clients that support the [MCPB format](https://github.com/modelcontextprotocol/mcpb)
- [lighthouse-skill.zip](https://github.com/LetPeopleWork/lighthouse-clients/releases/latest/download/lighthouse-skill.zip) for installing the Lighthouse agent skill directly as a reusable Lighthouse-specific guidance pack

Think of these as two different deliverables:

- `lighthouse-mcp-stdio.mcpb` is a packaged MCP server setup for clients that support MCPB
- `lighthouse-skill.zip` is a packaged Lighthouse skill for clients that support importing custom skills, prompt bundles, or agent capabilities

## Authentication and API Keys

If your Lighthouse instance runs without authentication, the examples on this page usually work with just the target URL.

If authentication is enabled, non-browser clients should use an API key. Create and manage those keys from [System Settings > API Keys](./settings/apikeys.html). The full authentication setup is described in [Authentication](./Installation/authentication.html).

{: .note}
API keys are intended for CLI usage, MCP servers, scripts, and other non-browser automation paths. Browser sign-in still uses the normal Lighthouse authentication flow.

Most client packages expect the key in the `LIGHTHOUSE_API_KEY` environment variable, or let you provide it during the CLI connection flow.

## CLI for Scripting and Automation

Use the CLI when you want a terminal-first workflow, a scriptable JSON interface, or a straightforward way for coding agents to access Lighthouse data.

### Install

```bash
npm install -g @letpeoplework/lighthouse-cli
```

You can also install it globally with pnpm:

```bash
pnpm add -g @letpeoplework/lighthouse-cli
```

If you prefer release installers instead of npm tooling, use the latest published installer assets.

Linux and macOS:

```bash
curl -fsSL https://github.com/LetPeopleWork/lighthouse-clients/releases/latest/download/install.sh | bash
```

Windows:

```powershell
irm https://github.com/LetPeopleWork/lighthouse-clients/releases/latest/download/install.ps1 | iex
```

{: .note}
The installer route is useful when you want the CLI available directly as `lh` without relying on `npx` or a local package install inside a project.

### Connect to a Server

```bash
lh connection connect --mode server --url https://lighthouse.example.com
```

If the target Lighthouse instance requires authentication, either pass the key during connect or inject it through the environment.

```bash
lh connection connect --mode server --url https://lighthouse.example.com --api-key <key>
```

```bash
LIGHTHOUSE_API_KEY=<key> lh connection connect --mode server --url https://lighthouse.example.com
```

For the local standalone desktop app, you can skip the URL and let the CLI discover Lighthouse automatically.

```bash
lh connection connect --mode standalone
```

### Script Example

The CLI supports machine-readable output through `--json`, which makes it a good fit for shell pipelines and CI.

```bash
LIGHTHOUSE_API_KEY=<key> \
lh metrics team --id 1 --metrics throughput,cycleTime --json
```

Another common automation pattern is to list teams, pick an ID, and run a forecast.

```bash
LIGHTHOUSE_API_KEY=<key> lh team list --json
LIGHTHOUSE_API_KEY=<key> lh forecast manual --team-id 1 --remaining 12 --json
```

## MCP for AI Clients

MCP lets AI tools call Lighthouse functionality as tools instead of relying on pasted screenshots or manually copied metrics. Lighthouse supports both a local stdio MCP server and a shared HTTP MCP server.

### MCP stdio for Local Clients

Use `@letpeoplework/lighthouse-mcp-stdio` when your AI client can launch a local process on the same machine.

You can start it directly with `npx`:

```bash
npx -y @letpeoplework/lighthouse-mcp-stdio
```

### VS Code / GitHub Copilot Example

Add the server to `.vscode/mcp.json` or your user MCP configuration:

```json
{
  "servers": {
    "lighthouse": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@letpeoplework/lighthouse-mcp-stdio"],
      "env": {
        "LIGHTHOUSE_URL": "https://lighthouse.example.com",
        "LIGHTHOUSE_API_KEY": "replace-me"
      }
    }
  }
}
```

If you are using the local Lighthouse standalone app, you can usually omit `LIGHTHOUSE_URL` and let the package discover the running instance.

### MCPB Bundle

If your client supports `.mcpb` bundles, the quickest setup is the latest [lighthouse-mcp-stdio.mcpb](https://github.com/LetPeopleWork/lighthouse-clients/releases/latest/download/lighthouse-mcp-stdio.mcpb). Download the file, open it in your MCP-capable client, and provide the Lighthouse URL and optional API key when prompted.

### MCP HTTP for Shared Setups

Use `@letpeoplework/lighthouse-mcp-http` when you want one hosted MCP endpoint that multiple users or clients can share.

Run it locally with `npx`:

```bash
LIGHTHOUSE_URL=https://lighthouse.example.com \
LIGHTHOUSE_API_KEY=replace-me \
HOST=127.0.0.1 \
PORT=3333 \
npx -y @letpeoplework/lighthouse-mcp-http
```

This exposes:

- `GET /health` for health checks
- `POST /mcp` for MCP requests

Client configuration then points to the MCP endpoint:

```json
{
  "servers": {
    "lighthouse": {
      "type": "http",
      "url": "http://127.0.0.1:3333/mcp"
    }
  }
}
```

## Run MCP HTTP in Docker

For containerized or remote setups, use the published Docker image:

```bash
docker run --rm \
  -p 3000:3000 \
  -e HOST=0.0.0.0 \
  -e PORT=3000 \
  -e LIGHTHOUSE_URL=https://lighthouse.example.com \
  -e LIGHTHOUSE_API_KEY=replace-me \
  ghcr.io/letpeoplework/lighthouse-clients/mcp-http:latest
```

Point your client to `http://<host>:3000/mcp` once the container is running.

{: .recommendation}
Use MCP stdio for personal local setups and MCP HTTP for shared or hosted setups. If your workflow is mostly shell-based, prefer the CLI.

## Lighthouse Agent Skill

The Lighthouse skill is its own deliverable, separate from the CLI and separate from the MCP server packages. It gives an LLM a ready-made kickstart for using Lighthouse effectively: when to use MCP tools, when to fall back to the CLI, how to connect to Lighthouse, and how to interpret Lighthouse flow metrics and forecasts.

You can install or inspect it here:

- [Agent Skills directory](https://agentskills.io/home)
- [Latest Lighthouse skill download](https://github.com/LetPeopleWork/lighthouse-clients/releases/latest/download/lighthouse-skill.zip)

### What the Skill Does

The skill does not replace the CLI or MCP. Instead, it teaches the LLM how to use those deliverables well.

Typical benefits include:

- Choosing the right access path: MCP tools first, CLI as fallback
- Guiding the model toward Lighthouse-specific commands and prompts
- Explaining how to authenticate with API keys when needed
- Improving the quality of flow-metrics and forecasting interpretation

### How to Use It

1. Import the [lighthouse-skill.zip](https://github.com/LetPeopleWork/lighthouse-clients/releases/latest/download/lighthouse-skill.zip) into a client that supports custom skills or prompt bundles.
2. Give the client access to Lighthouse through either the CLI or an MCP server.
3. Ask the model to use Lighthouse for a concrete task such as team metrics, forecasting, or portfolio analysis.

### Recommended Combinations

The skill works best together with one of the actual access mechanisms:

- **Skill + MCP stdio**: best for local VS Code / GitHub Copilot or Claude-style setups where the model can call Lighthouse tools directly
- **Skill + MCP HTTP**: best for shared or hosted AI environments where multiple clients connect to one MCP endpoint
- **Skill + CLI**: best for terminal-capable coding agents, shell workflows, and environments where MCP is not available

In short, the skill improves the model's Lighthouse behavior, while the CLI and MCP packages provide the real connectivity.

### Example Usage Pattern

One practical setup is:

1. Install the Lighthouse skill in the AI client.
2. Connect Lighthouse through MCP or the CLI.
3. Start with a task like: "Use Lighthouse to analyze the last 30 days of throughput and cycle time for team 1, then explain the delivery risks."

This is especially useful if your client supports importing custom skills or prompt bundles and you want the model to begin with Lighthouse-specific guidance instead of a blank prompt.

## Example Prompts and Workflows

Once connected through CLI or MCP, common workflows look like this:

- "List all Lighthouse teams and show me their IDs."
- "Run a forecast for team 1 with 12 remaining items."
- "Get throughput and cycle time for the last 30 days and summarize the risks."
- "Show portfolio metrics as raw JSON so I can use them in a script."

For a shell-first workflow, the CLI is often enough. For natural-language, tool-driven conversations inside an AI client, prefer MCP.

## Related Documentation

- [System Settings > API Keys](./settings/apikeys.html)
- [Authentication](./Installation/authentication.html)
- [Configuration](./settings/configuration.html)
- [System Settings](./settings/settings.html)