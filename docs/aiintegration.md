---
title: AI Integration
layout: home
nav_order: 40
---

If you're using AI clients like Claude or Copilot, you can configure Lighthouse as a resource for those clients to get data from. Read on to learn how.

- TOC
{:toc}

{: .important}
Be aware that this topic is very fast moving. We're trying to keep the docs up to date, but things may change. If you discover something outdated, please let us know via Slack.

{: .recommendation}
This feature is still in preview and freely available. It's planned that this will become a premium-only feature once it's out of preview.

# Model Context Protocol
The [Model Context Protocol](https://modelcontextprotocol.io/introduction) (MCP) is a standard that defines how applications can provide context to Large Language Models (LLMs). This allows to *extend* the knowledge of this LLM to include context from other applications, like Lighthouse. The use of this standardized protocol means that the feature can be implemented generically in Lighthouse, and if your LLM supports MCP already (you can check [here](https://modelcontextprotocol.io/clients)), you can use it.

Lighthouse can act as an *MCP Server*, meaning that you can tell your LLM to run forecasts for a team, or analyze flow metrics. All from the comfort of your LLMs chat! Pretty cool stuff, isn't it?

# Enable MCP Feature
The MCP Feature is not enabled by default. In order to do so, go to the Settings and enable the *MCP Server*.

{: .note}
You need to restart Lighthouse for the change to take effect.

![MCP Feature](./assets/settings/optionalfeatures.png)

# Connect to Lighthouse MCP Server
Once the MCP Server is enabled and you restarted Lighthouse, you can add it as MCP Server to your LLM. How to do this varies by LLM, so it's best to check the configuration of your tool.

Lighthouse is using *Server Side Events (SSE)* as technology, so you don't need to install anything extra on your machine, just point to the place where Lighthouse is running.

## Example Configurations
Following are some example configurations. We're assuming that Lighthouse is running on your local machine, on port 8080.

## VS Code Copilot
VS Code Copilot supports MCP Servers via HTTP transport. Use this configuration:

```json
    "mcp": {
        "servers": {
            "lighthouse-mcp": {
                "type": "http",
                "url": "http://localhost:8080"
            }
        }
    },
```

## Claude Desktop
Claude Desktop supports MCP via HTTP transport. Use this configuration:

```json
    "mcpServers": {
        "Lighthouse": {
            "command": "npx",
            "args": [
                "mcp-remote",
                "http://localhost:8080"
            ]
        }
    }
```

# Available MCP Features
The Model Context Protocol supports different types of actions. *Resources* to get data from the server, *Prompts* to get predefined prompts, and *Tools* to execute actions.

Lighthouse only implements *Tools*, as this is the type that is most widely supported by the clients.

## Tools
Tools can be used by your LLM out of the context, or as a specific action (for example after a `/` or `#` in the text).

The following tools are available on the Lighthouse MCP Server:

- *GetAllTeams*: Gets all teams configured in Lighthouse
- *GetTeamByName*: Gets a specific team with details by name
- *RunHowManyForecast*: Runs a forecast for a specified team and a given date range to identify how many items can be closed in this time
- *RunWhenForecast*: Runs a forecast for a specified team and a given number of items to identify when they will be closed
- *GetFlowMetricsForTeam*: Gets the metrics for the specified team
