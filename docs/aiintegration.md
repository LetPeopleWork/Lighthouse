---
title: AI Integration
layout: home
nav_order: 40
---

If you're using AI clients like Claude or Copilot, you can configure Lighthouse as a resource for those clients to get data from. This comprehensive guide will help you successfully configure and use the Lighthouse MCP server with your preferred AI client.

- TOC
{:toc}

{: .important}
Be aware that this topic is very fast moving. We're trying to keep the docs up to date, but things may change. If you discover something outdated, please let us know via Slack.

{: .recommendation}
This feature is still in preview and freely available. It's planned that this will become a premium-only feature once it's out of preview.

# Model Context Protocol

The [Model Context Protocol](https://modelcontextprotocol.io/introduction) (MCP) is a standard that defines how applications can provide context to Large Language Models (LLMs). This allows to *extend* the knowledge of this LLM to include context from other applications, like Lighthouse. The use of this standardized protocol means that the feature can be implemented generically in Lighthouse, and if your LLM supports MCP already (you can check [here](https://modelcontextprotocol.io/clients)), you can use it.

Lighthouse can act as an *MCP Server*, meaning that you can tell your LLM to run forecasts for a team, analyze flow metrics, and access comprehensive documentation. All from the comfort of your LLMs chat! Pretty cool stuff, isn't it?

## What Can You Do With Lighthouse MCP?

The Lighthouse MCP server enables your AI assistant to:

- **Team Analytics**: Get team information, run forecasts, and analyze flow metrics
- **Project Management**: Access project details, features, milestones, and completion forecasts  
- **Feature Tracking**: Get detailed feature information and delivery forecasts
- **Expert Guidance**: Access specialized prompts for agile coaching and flow analysis
- **Documentation**: Read comprehensive Lighthouse documentation for context

## Supported Use Cases

### Team-Focused Analysis
- "How many items can my team complete by the end of the month?"
- "When will we finish the remaining 15 items in our backlog?"
- "Analyze our team's flow metrics for the last 30 days"
- "What aging items need attention and why?"

### Project-Level Insights  
- "When will Project Alpha be completed?"
- "Show me the milestone likelihood for our Q1 release"
- "Which teams are working on Project Beta?"
- "What features are planned for the next release?"

### Flow Coaching & Improvement
- "Analyze our cycle time trends and suggest improvements"
- "Help me prepare a data-driven retrospective"
- "Generate forecasting insights for our planning meeting"
- "Interpret our flow metrics using actionable agile principles"

# Enable MCP Feature
The MCP Feature is not enabled by default. In order to do so, go to the Settings and enable the *MCP Server*.

{: .note}
You need to restart Lighthouse for the change to take effect.

![MCP Feature](./assets/settings/optionalfeatures.png)

# Connect to Lighthouse MCP Server

Once the MCP Server is enabled and you restarted Lighthouse, you can add it as MCP Server to your LLM. How to do this varies by LLM, so it's best to check the configuration of your tool.

Lighthouse uses *Server Side Events (SSE)* as technology, so you don't need to install anything extra on your machine, just point to the place where Lighthouse is running.

{: .note}
The default Lighthouse MCP server runs on the same port as your main Lighthouse application. If Lighthouse is running on `http://localhost:8080`, then the MCP server is available at the same URL.

## Popular Client Configurations

The following sections provide step-by-step configuration examples for popular MCP clients. Choose the section that matches your preferred AI assistant.

### Claude Desktop

Claude Desktop supports MCP via HTTP transport and is one of the most popular clients for MCP integration.

**Step 1**: Open Claude Desktop Settings
- Click on the Claude menu in your system's menu bar (macOS) or application menu (Windows)
- Select "Settings..."

**Step 2**: Access Developer Settings  
- Navigate to the "Developer" tab in the left sidebar
- Click the "Edit Config" button to open the configuration file

**Step 3**: Add Lighthouse Configuration
Replace or add to the configuration file:

```json
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "http://localhost:8080"
      ]
    }
  }
}
```

**Step 4**: Restart Claude Desktop
Completely quit and restart Claude Desktop. You should see an MCP server indicator in the bottom-right corner of the chat input.

### VS Code Copilot

VS Code Copilot supports MCP Servers via HTTP transport with excellent integration into your development workflow.

**Step 1**: Open VS Code Settings
- Press `Ctrl+,` (Windows/Linux) or `Cmd+,` (macOS) to open settings
- Switch to JSON view by clicking the "Open Settings (JSON)" icon

**Step 2**: Add MCP Configuration
Add the following to your VS Code settings:

```json
{
  "mcp": {
    "servers": {
      "lighthouse-mcp": {
        "type": "http",
        "url": "http://localhost:8080"
      }
    }
  }
}
```

**Step 3**: Restart VS Code
Reload your VS Code window or restart the application for the changes to take effect.

### Continue (VS Code Extension)

Continue is an open-source AI code assistant with full MCP support.

**Step 1**: Install Continue Extension
- Open VS Code and go to the Extensions view (`Ctrl+Shift+X`)
- Search for "Continue" and install the extension

**Step 2**: Configure MCP Server
- Open the Continue configuration file (`.continue/config.json` in your workspace)
- Add the Lighthouse MCP server:

```json
{
  "mcpServers": {
    "lighthouse": {
      "transport": {
        "type": "http",
        "url": "http://localhost:8080"
      }
    }
  }
}
```

### GitHub Copilot Coding Agent

GitHub Copilot supports MCP for enhanced context and capabilities.

**Step 1**: Configure via MCP Settings
In your repository, create or update `.mcp/config.json`:

```json
{
  "servers": {
    "lighthouse": {
      "url": "http://localhost:8080",
      "description": "Lighthouse project management and forecasting tools"
    }
  }
}
```

### Other HTTP-Based Clients

For any MCP client that supports HTTP/SSE transport, use these general guidelines:

**Transport Type**: HTTP or SSE  
**URL**: `http://localhost:8080` (replace with your Lighthouse instance URL)  
**Authentication**: None required for local instances  

## Remote Server Configuration

If you're running Lighthouse on a remote server, adjust the configuration accordingly:

```json
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": [
        "mcp-remote", 
        "https://your-lighthouse-instance.com"
      ]
    }
  }
}
```

{: .warning}
**Security Consideration**: When connecting to remote Lighthouse instances, ensure they are properly secured with HTTPS and appropriate authentication mechanisms.

## Configuration Verification

After configuring your MCP client:

1. **Check Connection**: Look for MCP server indicators in your client (usually a tool icon or status indicator)
2. **Test Tools**: Try asking your AI assistant to "list all teams" or "get project information"
3. **Review Logs**: Check your client's developer console or logs for any connection errors
4. **Verify Resources**: Ask your AI to access Lighthouse documentation to confirm resource loading

# Available MCP Features

The Model Context Protocol supports different types of actions: *Tools* for executing actions, *Prompts* for predefined expert guidance, and *Resources* for accessing documentation.

Lighthouse implements a comprehensive set of capabilities across all three categories, making it one of the most feature-complete MCP servers available.

## Tools

Tools allow your AI assistant to execute specific actions and retrieve data from Lighthouse. All tools return structured JSON data that your AI can interpret and explain in natural language.

### Team Management Tools

**GetAllTeams**
- **Purpose**: Retrieves a list of all teams configured in Lighthouse
- **Usage**: "Show me all teams" or "List available teams"
- **Returns**: Team ID, name, and basic metadata
- **Example Response**:
```json
[
  {"Id": 1, "Name": "Alpha Team"},
  {"Id": 2, "Name": "Beta Team"}
]
```

**GetTeamByName**
- **Purpose**: Gets detailed information about a specific team
- **Parameters**: `teamName` (string) - Name or partial name of the team
- **Usage**: "Get details about the Alpha team"
- **Returns**: Complete team configuration including work tracking connections and settings
- **Example**: `GetTeamByName("Alpha")` returns full team configuration

**GetFlowMetricsForTeam**
- **Purpose**: Retrieves comprehensive flow metrics for a team within a date range
- **Parameters**: 
  - `teamName` (string) - Name of the team
  - `startDate` (DateTime, optional) - Start date (defaults to 30 days ago)
  - `endDate` (DateTime, optional) - End date (defaults to today)
- **Usage**: "Analyze Alpha team's metrics for the last quarter"
- **Returns**: Cycle time percentiles, throughput data, WIP metrics, and individual cycle times
- **Business Value**: Enables flow analysis, bottleneck identification, and predictability assessment

### Team Forecasting Tools

**RunWhenForecast**
- **Purpose**: Generates probabilistic forecasts for when a team will complete a given number of items
- **Parameters**: 
  - `teamName` (string) - Name of the team
  - `remainingItems` (int) - Number of items to forecast
- **Usage**: "When will Alpha team finish 25 remaining items?"
- **Returns**: Monte Carlo simulation results with 50th, 70th, 85th, and 95th percentile forecasts
- **Business Value**: Data-driven planning and realistic deadline setting

**RunHowManyForecast**
- **Purpose**: Forecasts how many items a team can complete by a specific date
- **Parameters**: 
  - `teamName` (string) - Name of the team
  - `untilWhen` (DateTime) - Target completion date
- **Usage**: "How many items can Beta team complete by end of quarter?"
- **Returns**: Probabilistic item count forecasts with confidence intervals
- **Business Value**: Capacity planning and scope management

### Project Management Tools

**GetAllProjects**
- **Purpose**: Lists all projects with summary information
- **Usage**: "Show me all active projects"
- **Returns**: Project names, team counts, feature counts, and milestone counts
- **Business Value**: Portfolio visibility and project discovery

**GetProjectByName**
- **Purpose**: Retrieves detailed information about a specific project
- **Parameters**: `projectName` (string) - Name or partial name of the project
- **Usage**: "Get details about Project Apollo"
- **Returns**: Complete project configuration, team assignments, feature counts, and settings
- **Example**: `GetProjectByName("Apollo")` returns comprehensive project data

**GetProjectFeatures**
- **Purpose**: Lists all features within a specific project
- **Parameters**: `projectName` (string) - Name of the project
- **Usage**: "What features are in Project Apollo?"
- **Returns**: Feature names, states, owners, work item counts, and progress status
- **Business Value**: Feature portfolio visibility and progress tracking

**GetProjectTeams**
- **Purpose**: Shows which teams are involved in a specific project
- **Parameters**: `projectName` (string) - Name of the project
- **Usage**: "Which teams are working on Project Apollo?"
- **Returns**: Team names, IDs, and their relationship to the project (including owning team)
- **Business Value**: Resource allocation visibility and team coordination

**GetProjectMilestones**
- **Purpose**: Analyzes project milestones with likelihood assessments
- **Parameters**: `projectName` (string) - Name of the project
- **Usage**: "Analyze milestones for Project Apollo"
- **Returns**: Milestone dates, completion likelihoods, risk assessments, and recommendations
- **Business Value**: Risk management and stakeholder communication

**RunProjectWhenForecast**
- **Purpose**: Forecasts when an entire project will be completed
- **Parameters**: `projectName` (string) - Name of the project
- **Usage**: "When will Project Apollo be completed?"
- **Returns**: Project-level completion forecasts based on critical path analysis
- **Business Value**: Executive reporting and project portfolio planning

**GetProjectFlowMetrics**
- **Purpose**: Analyzes flow metrics at the project level across multiple teams
- **Parameters**: 
  - `projectName` (string) - Name of the project
  - `startDate` (DateTime, optional) - Analysis start date
  - `endDate` (DateTime, optional) - Analysis end date
- **Usage**: "Analyze Project Apollo's flow metrics for Q3"
- **Returns**: Aggregated cycle times, throughput, WIP, and multi-team flow analysis
- **Business Value**: Project health assessment and flow optimization

### Feature Management Tools

**GetFeatureDetails**
- **Purpose**: Retrieves comprehensive information about a specific feature
- **Parameters**: `featureName` (string) - Name of the feature
- **Usage**: "Get details about the User Authentication feature"
- **Returns**: Feature state, work breakdown, team assignments, size estimates, and blocking status
- **Business Value**: Feature-level visibility and detailed planning

**GetFeatureWhenForecast**
- **Purpose**: Forecasts when a specific feature will be completed
- **Parameters**: `featureName` (string) - Name of the feature
- **Usage**: "When will the Payment Integration feature be done?"
- **Returns**: Feature-specific completion forecasts with 50/70/85/95 percentile confidence intervals
- **Business Value**: Feature-level planning and dependency management

## Expert Prompts

Lighthouse provides specialized prompts designed by agile coaching experts following "Actionable Agile Metrics for Predictability" and ProKanban.org principles.

### Flow Analysis Prompts

**AnalyzeTeamMetrics**
- **Purpose**: Provides expert analysis of team flow using the three core metrics
- **Parameters**: 
  - `teamName` (string) - Team to analyze
  - `dateRange` (string, optional) - Period to analyze
- **Expert Context**: Pro Kanban trainer perspective focusing on throughput, cycle time, and WIP
- **Usage**: Ask your AI to analyze team metrics using proven flow principles
- **Value**: Professional-grade flow analysis without needing coaching expertise

**AnalyzeTeamAging**
- **Purpose**: Identifies aging work items and flow problems using actionable agile metrics
- **Parameters**: 
  - `teamName` (string) - Team to analyze
  - `agingThresholdDays` (int) - Days to consider items as aging (default: 10)
- **Expert Context**: Focuses on flow problems and predictability issues
- **Usage**: "Analyze aging items for Alpha team"
- **Value**: Proactive identification of flow bottlenecks and systemic issues

**GenerateForecastingInsights**
- **Purpose**: Provides probabilistic forecasting guidance using Monte Carlo principles
- **Parameters**: 
  - `teamName` (string) - Team to forecast for
  - `forecastTarget` (string) - Number of items or target date
  - `forecastType` (string) - "when" or "howmany"
- **Expert Context**: Based on proven forecasting methodologies and historical data analysis
- **Usage**: "Generate forecasting insights for Beta team with 30 items"
- **Value**: Data-driven forecasting with expert interpretation

**AnalyzeProjectCompletion**
- **Purpose**: Analyzes project completion using portfolio flow metrics
- **Parameters**: `projectName` (string) - Project to analyze
- **Expert Context**: Portfolio-level flow analysis considering team interdependencies
- **Usage**: "Analyze Project Apollo completion dynamics"
- **Value**: Executive-level insights on project delivery probability

**GenerateDataDrivenInsights**
- **Purpose**: Facilitates data-driven retrospectives using actionable agile metrics
- **Parameters**: 
  - `teamName` (string) - Team for retrospective
  - `timePeriod` (string) - Analysis period (e.g., "last 30 days")
- **Expert Context**: ProKanban.org retrospective approach focusing on learning and improvement
- **Usage**: "Generate retrospective insights for Alpha team"
- **Value**: Structured, data-driven team improvement sessions

## Resources

Resources provide your AI assistant with direct access to Lighthouse documentation, enabling contextual responses about configuration, usage, and best practices.

### Available Documentation Resources

**Lighthouse Documentation**
- **Content**: Main documentation homepage with overview and getting started information
- **URI**: `/docs/index.html`
- **Usage**: Provides foundational knowledge about Lighthouse capabilities

**Core Concepts**
- **Content**: Teams, projects, forecasting methodology, and flow metrics explanation
- **URI**: `/docs/concepts/concepts.html`
- **Usage**: Helps AI understand Lighthouse's fundamental concepts

**Team Management**
- **Content**: How to set up, configure, and manage teams in Lighthouse
- **URI**: `/docs/teams/teams.html`
- **Usage**: Provides guidance on team setup and configuration

**Project Management**
- **Content**: Project creation, feature management, and milestone tracking
- **URI**: `/docs/projects/projects.html`
- **Usage**: Comprehensive project management guidance

**Metrics & Analytics**
- **Content**: Understanding flow metrics, dashboard configuration, and interpretation
- **URI**: `/docs/metrics/metrics.html`
- **Usage**: Deep dive into metrics analysis and dashboard usage

**AI Integration Guide**
- **Content**: This current documentation on MCP integration and usage
- **URI**: `/docs/aiintegration.html`
- **Usage**: Self-referential guide for MCP setup and troubleshooting

**Forecasting Methodology**
- **Content**: Detailed explanation of Monte Carlo simulations and statistical methods
- **URI**: `/docs/concepts/howlighthouseforecasts.html`
- **Usage**: Technical deep dive into Lighthouse's forecasting approach

### Resource Benefits

1. **Contextual Responses**: Your AI can reference official documentation when answering questions
2. **Accurate Information**: Ensures responses are based on current, authoritative sources
3. **Comprehensive Coverage**: Access to all aspects of Lighthouse functionality
4. **Self-Service Support**: Reduces need for manual documentation lookup
5. **Consistent Guidance**: Standardized responses based on official documentation

## Tool Integration Patterns

### Workflow Examples

**Team Health Check**:
1. `GetTeamByName("Alpha")` - Get team overview
2. `GetFlowMetricsForTeam("Alpha", startDate, endDate)` - Analyze metrics
3. `AnalyzeTeamMetrics("Alpha")` - Expert interpretation
4. `AnalyzeTeamAging("Alpha")` - Identify problem areas

**Project Status Review**:
1. `GetProjectByName("Apollo")` - Project overview
2. `GetProjectFeatures("Apollo")` - Feature inventory
3. `GetProjectMilestones("Apollo")` - Risk assessment
4. `RunProjectWhenForecast("Apollo")` - Completion forecast

**Sprint Planning Session**:
1. `GetFlowMetricsForTeam("Beta")` - Historical performance
2. `RunHowManyForecast("Beta", sprintEndDate)` - Capacity forecast
3. `GenerateForecastingInsights("Beta", "capacity")` - Expert guidance

**Executive Reporting**:
1. `GetAllProjects()` - Portfolio overview
2. `GetProjectMilestones(projectName)` for each project - Risk dashboard
3. `AnalyzeProjectCompletion(projectName)` - Expert analysis per project

These tools, prompts, and resources work together to provide comprehensive project management intelligence through your AI assistant, combining real-time data with expert knowledge for informed decision-making.

# Troubleshooting Guide

This section covers common issues when setting up and using the Lighthouse MCP server, along with their solutions.

## Connection Issues

### MCP Server Not Showing Up in Client

**Symptoms**: No MCP server indicator appears in your AI client after configuration.

**Possible Causes & Solutions**:

1. **Lighthouse MCP Feature Not Enabled**
   - Go to Lighthouse Settings > Optional Features
   - Enable "MCP Server" feature
   - Restart Lighthouse application
   - Verify the feature is active in the UI

2. **Client Configuration Errors**
   - Verify JSON syntax in your client configuration file
   - Ensure URLs match your Lighthouse instance (check port numbers)
   - For local instances, use `http://localhost:8080` (replace 8080 with your port)
   - For remote instances, ensure HTTPS is used

3. **Client Restart Required**
   - Completely quit and restart your AI client application
   - Some clients require full restart to reload MCP configurations
   - Check client-specific restart procedures

4. **Network Connectivity**
   - Verify Lighthouse is running and accessible via browser
   - Check firewall settings allow connections on Lighthouse port
   - For remote instances, verify network connectivity

### Tools Not Loading or Showing as Unavailable

**Symptoms**: MCP connection shows but tools don't appear or return errors.

**Possible Causes & Solutions**:

1. **Lighthouse Data Not Loaded**
   - Ensure you have at least one team configured in Lighthouse
   - Verify work tracking system connections are active
   - Check that data synchronization has completed

2. **Permission Issues**
   - Verify Lighthouse is running with appropriate permissions
   - For remote instances, check authentication requirements
   - Ensure MCP server has access to Lighthouse database

3. **Version Compatibility**
   - Verify you're running a compatible version of Lighthouse
   - MCP support requires Lighthouse version 1.5.0 or later
   - Check client MCP protocol version compatibility

### Authentication and Security Issues

**Symptoms**: Connection refused, unauthorized access, or certificate errors.

**Solutions**:

1. **Local Instance Security**
   - Local Lighthouse instances typically don't require authentication
   - Ensure firewall allows localhost connections
   - Verify antivirus software isn't blocking connections

2. **Remote Instance Authentication**
   - Contact your Lighthouse administrator for authentication details
   - Use HTTPS URLs for remote instances
   - Verify SSL certificates are valid and trusted

3. **Corporate Network Issues**
   - Check proxy settings in your AI client
   - Verify corporate firewall allows MCP connections
   - Contact IT department if persistent connection issues occur

## Configuration Problems

### JSON Configuration Errors

**Common Syntax Issues**:

```json
// ❌ WRONG - Missing comma
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": ["mcp-remote", "http://localhost:8080"]
    }
    "another-server": {...}
  }
}

// ✅ CORRECT - Proper comma placement
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": ["mcp-remote", "http://localhost:8080"]
    },
    "another-server": {...}
  }
}
```

**Validation Steps**:
1. Use JSON validator tools to check syntax
2. Copy exact examples from this documentation
3. Pay attention to comma placement and bracket matching
4. Use JSON formatting tools for readability

### Path and URL Configuration

**Common URL Mistakes**:

```json
// ❌ WRONG Examples
"http://localhost"           // Missing port
"https://localhost:8080"     // Wrong protocol for local
"http://myserver"            // Missing port for remote
"lighthouse.local:8080"      // Missing protocol

// ✅ CORRECT Examples  
"http://localhost:8080"      // Local with port
"https://lighthouse.company.com"  // Remote with HTTPS
"http://192.168.1.100:8080" // Local network with port
```

**Port Detection**:
- Check Lighthouse startup logs for port information
- Default port is usually 8080, but may vary
- Verify port in browser address bar when accessing Lighthouse

## Runtime Errors

### Tool Execution Failures

**Symptoms**: Tools appear but return error messages when executed.

**Diagnostic Steps**:

1. **Check Tool Parameters**
   - Verify team/project names exist in Lighthouse
   - Use exact names or check for typos
   - Team and project names are case-insensitive but must match

2. **Data Availability**
   - Ensure teams have been synchronized with work tracking systems
   - Verify projects have features and teams assigned
   - Check that forecasting data is available (may take time after setup)

3. **Common Error Messages**:
   - "No team found with name X" - Check team name spelling
   - "No forecast data available" - Wait for data synchronization or check team configuration
   - "Project has no features" - Verify project setup and feature assignment

### Performance Issues

**Symptoms**: Slow response times or timeouts.

**Solutions**:

1. **Large Dataset Optimization**
   - Use date ranges for metric queries to limit data volume
   - Request specific teams/projects rather than all data
   - Consider increasing client timeout settings

2. **System Resources**
   - Verify Lighthouse server has adequate memory and CPU
   - Check for other resource-intensive processes
   - Monitor database performance if using external database

## Client-Specific Issues

### Claude Desktop Issues

**Common Problems**:
- Configuration file not found: Use "Edit Config" button in Developer settings
- Server starts but no tools appear: Restart Claude Desktop completely
- "npx" command not found: Install Node.js and verify PATH

**Configuration File Locations**:
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`

### VS Code Copilot Issues

**Common Problems**:
- MCP settings not recognized: Ensure VS Code version supports MCP
- Configuration ignored: Restart VS Code window or reload application
- Tools don't appear in chat: Check developer console for errors

### Continue Extension Issues

**Common Problems**:
- Extension not loading MCP config: Check `.continue/config.json` syntax
- Connection refused: Verify Lighthouse URL accessibility
- Tools timeout: Adjust timeout settings in Continue configuration

## Diagnostic Commands

### Verify Lighthouse MCP Endpoint

Test if the MCP endpoint is accessible:

```bash
# Check if Lighthouse is running
curl http://localhost:8080/health

# Test MCP endpoint availability
curl http://localhost:8080/mcp/health
```

### Check MCP Client Logs

Most MCP clients provide diagnostic logs:

- **Claude Desktop**: Check Console application (macOS) or Event Viewer (Windows)
- **VS Code**: Open Developer Console (Help > Toggle Developer Tools)
- **Continue**: Check VS Code Output panel, select "Continue" channel

### Network Connectivity Tests

```bash
# Test basic connectivity
ping localhost

# Test port accessibility
telnet localhost 8080

# Test HTTPS connectivity (for remote instances)
curl -I https://your-lighthouse-instance.com
```

## Getting Additional Help

If you've tried the troubleshooting steps above and still encounter issues:

### Community Support

1. **Slack Community**: Join our [Slack workspace](https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A) for community support
2. **Documentation**: Check the full [Lighthouse documentation](https://docs.lighthouse.letpeople.work)
3. **GitHub Issues**: Search existing issues or create new ones at [GitHub Repository](https://github.com/LetPeopleWork/Lighthouse)

### Information to Include in Support Requests

When asking for help, please provide:

1. **Environment Details**:
   - Lighthouse version
   - Operating system
   - MCP client and version
   - Network setup (local/remote)

2. **Configuration**:
   - Sanitized MCP configuration (remove sensitive URLs)
   - Lighthouse optional features settings
   - Error messages or logs

3. **Steps to Reproduce**:
   - Exact steps you followed
   - Expected vs. actual behavior
   - Screenshots if helpful

4. **Diagnostic Information**:
   - Results from diagnostic commands above
   - Client error logs
   - Lighthouse application logs

### Professional Support

For enterprise users or complex setups, consider:

- **Professional Services**: Contact [LetPeopleWork](https://letpeople.work) for consulting
- **Enterprise Support**: Dedicated support packages available
- **Custom Integration**: Assistance with complex MCP configurations

Remember: The Lighthouse MCP integration is actively developed, and the community is very helpful in resolving issues quickly.

# Real-World Usage Scenarios

This section demonstrates practical applications of the Lighthouse MCP integration across different roles and use cases.

## Agile Coach / Scrum Master Scenarios

### Sprint Planning Enhancement
**Scenario**: Using historical data for realistic sprint planning

**Conversation Example**:
```
User: "Help me plan the next sprint for Alpha team. We have 25 items in our backlog."

AI Response: [Calls GetFlowMetricsForTeam and RunHowManyForecast]
"Based on Alpha team's historical performance over the last 30 days, here's what I found:

- Average cycle time: 3.2 days (50th percentile)
- Throughput: 8-12 items per 2-week sprint
- Current WIP: 6 items

For sprint planning, I recommend:
- Plan for 8-10 items (conservative estimate)
- Reserve 2-3 items as stretch goals
- Focus on completing current WIP before starting new work

The data shows Alpha team has consistent throughput when WIP stays below 8 items."
```

### Team Retrospective Facilitation
**Scenario**: Data-driven retrospective discussions

**Conversation Example**:
```
User: "I'm facilitating a retrospective for Beta team. What should we focus on?"

AI Response: [Calls AnalyzeTeamMetrics and AnalyzeTeamAging]
"Based on Beta team's metrics from the last 30 days, here are key discussion points:

🔍 **Flow Analysis**:
- Cycle time increased 40% compared to previous month
- 3 items are aging beyond 14 days
- Throughput dropped from 10 to 6 items/sprint

📊 **Suggested Retrospective Topics**:
1. What caused the cycle time increase?
2. How can we address the aging items?
3. Are there new blockers we haven't identified?
4. Has our WIP limit become ineffective?

💡 **Data-Driven Questions**:
- 'What happened around day 15 when our flow slowed?'
- 'Are the aging items related to external dependencies?'
- 'Has our definition of done changed?'"
```

## Product Manager Scenarios

### Feature Delivery Planning
**Scenario**: Planning feature rollout and managing stakeholder expectations

**Conversation Example**:
```
User: "When will the User Authentication feature be ready? I need to update stakeholders."

AI Response: [Calls GetFeatureWhenForecast]
"Based on current progress and team velocity:

📅 **Delivery Forecast**:
- 50% confidence: 12 days (March 15)
- 70% confidence: 18 days (March 21) 
- 85% confidence: 25 days (March 28)

🎯 **Recommendation**: 
Tell stakeholders to expect delivery by March 21st for a realistic timeline with good confidence. Use March 28th for any external dependencies or announcements.

📊 **Current Status**:
- 8 of 15 work items completed
- Alpha team working at normal velocity
- No current blockers identified"
```

### Portfolio-Level Planning
**Scenario**: Managing multiple projects and resource allocation

**Conversation Example**:
```
User: "I need an executive summary of all our projects for the board meeting."

AI Response: [Calls GetAllProjects, then GetProjectMilestones for each]
"Here's your portfolio status for the board:

🚦 **Project Health Summary**:

**Project Apollo** ✅ Healthy
- Q2 launch: 85% confidence 
- 3 teams, 12 features remaining
- All milestones tracking well

**Project Beta** ⚠️ At Risk  
- Q2 deadline: 45% confidence
- Critical path: Payment integration
- Recommend scope reduction or timeline adjustment

**Project Gamma** ✅ Ahead of Schedule
- Q3 target: 95% confidence
- Could be delivered Q2 with current velocity

🎯 **Board Recommendations**:
1. Celebrate Gamma early delivery
2. Address Beta project risks immediately
3. Consider resource reallocation if needed"
```

## Engineering Manager Scenarios

### Team Performance Analysis
**Scenario**: Understanding team dynamics and bottlenecks

**Conversation Example**:
```
User: "My team seems to be struggling lately. Can you help me understand what's happening?"

AI Response: [Calls GetFlowMetricsForTeam and AnalyzeTeamAging]
"Let me analyze your team's recent performance data:

📈 **Performance Trends**:
- Throughput declined 30% over last month
- Cycle time variance increased significantly
- 5 items aging beyond normal patterns

🔍 **Potential Issues Identified**:
1. **Increased WIP**: Currently 12 items in progress (up from usual 8)
2. **Aging Pattern**: Items stall in 'In Review' status
3. **Variation**: Wide spread in completion times suggests inconsistent work size

💡 **Suggested Actions**:
- Implement WIP limits on review column
- Investigate review process bottlenecks
- Consider breaking down larger work items
- Schedule team discussion on work sizing practices"
```

### Resource Planning
**Scenario**: Cross-team coordination and resource allocation

**Conversation Example**:
```
User: "We're planning to move John from Alpha team to Beta team. What impact will this have?"

AI Response: [Calls GetFlowMetricsForTeam for both teams]
"Here's the potential impact analysis:

⚖️ **Alpha Team Impact**:
- Current velocity: 10 items/sprint with 5 members
- Projected velocity after change: 8 items/sprint
- Risk: May affect Q2 deliverables if no backfill

💪 **Beta Team Impact**:
- Current velocity: 6 items/sprint with 4 members  
- Projected velocity with John: 8-9 items/sprint
- Benefit: Could improve their Q2 delivery confidence

🎯 **Recommendations**:
1. Plan the transition after Alpha completes current sprint
2. Have John start with Beta team's smaller items first
3. Monitor both teams for 2-3 sprints post-transition
4. Consider temporary overlap period for knowledge transfer"
```

## Executive / Leadership Scenarios

### Strategic Planning Sessions
**Scenario**: Making data-driven portfolio decisions

**Conversation Example**:
```
User: "We're considering adding a new major feature to Project Apollo. How will this affect our roadmap?"

AI Response: [Calls RunProjectWhenForecast and GetProjectMilestones]
"Let me analyze the current trajectory and potential impact:

📊 **Current Apollo Status**:
- Baseline completion: July 15 (70% confidence)
- Critical milestones: Q2 Beta (95% confidence), Q3 GA (70% confidence)

⚠️ **Impact of Major Feature Addition**:
- Estimated delay: 3-4 weeks
- New completion: August 10-15 (70% confidence)
- Q3 GA milestone: Now at 45% confidence

💰 **Strategic Implications**:
- Pushes GA into Q4, affecting revenue projections
- May impact competitor response timing
- Could require additional team resources

🎯 **Executive Options**:
1. **Proceed**: Accept Q4 delivery with enhanced feature set
2. **Scope Reduction**: Remove lower-priority features to maintain timeline
3. **Resource Addition**: Add dedicated team to maintain timeline
4. **Phase Approach**: Deliver base version Q3, enhance version Q4"
```

### Board Reporting
**Scenario**: Regular executive reporting with predictive insights

**Conversation Example**:
```
User: "I need the monthly portfolio report for the board. Focus on delivery confidence and risks."

AI Response: [Comprehensive analysis across all projects]
"**Monthly Portfolio Report - March 2024**

🎯 **Delivery Confidence Summary**:
- 3 of 5 projects tracking to planned timelines (85%+ confidence)
- 1 project at moderate risk (60% confidence)  
- 1 project requiring immediate attention (30% confidence)

📊 **Key Metrics**:
- Overall portfolio velocity: Stable month-over-month
- Cross-project dependencies: 2 critical paths identified
- Resource utilization: 85% (healthy level)

⚠️ **Executive Attention Required**:
1. **Project Beta**: Payment integration blocking Q2 milestone
2. **Team Capacity**: Delta team approaching burnout indicators
3. **Dependencies**: External API availability affecting 2 projects

🚀 **Positive Indicators**:
- Project Gamma ahead of schedule (could accelerate market entry)
- Team Alpha showing improved predictability
- Technical debt reduction initiative showing ROI"
```

## Demo Video Walkthrough

A comprehensive video demonstration is available showing real-world usage scenarios. The video covers:

### Video Contents (15 minutes total)

**Part 1: Setup and Configuration (3 minutes)**
- Enabling MCP in Lighthouse settings
- Configuring Claude Desktop connection
- Verifying tool availability

**Part 2: Team Analysis Deep Dive (5 minutes)**
- Analyzing team metrics with expert prompts
- Interpreting cycle time trends
- Identifying flow bottlenecks
- Using forecasting for sprint planning

**Part 3: Project Management Workflows (4 minutes)**
- Multi-project portfolio analysis
- Feature delivery forecasting
- Milestone risk assessment
- Cross-team coordination insights

**Part 4: Executive Reporting (3 minutes)**
- Generating board-ready summaries
- Predictive insights for strategic planning
- Risk identification and mitigation planning

### Video Access

{: .note}
**Demo Video Coming Soon**: A comprehensive 15-minute video demonstration will be created showing real-world MCP usage scenarios. The video will be available at the Lighthouse documentation site once recording is complete. In the meantime, you can request a live demonstration through our [Slack community](https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A).

**Recording Planned**: The demo video is scheduled for creation and will include:
- Step-by-step configuration walkthroughs
- Live demonstrations of each tool and prompt
- Real data analysis scenarios
- Troubleshooting common issues
- Best practices for different use cases

The video will be linked here once available and will be promoted through the Lighthouse community channels.

### Key Demonstration Points

The video specifically demonstrates:

1. **Natural Language Interface**: How to ask complex questions without knowing exact tool names
2. **Data Interpretation**: How AI transforms raw metrics into actionable insights
3. **Expert Guidance Integration**: How prompts provide professional-grade analysis
4. **Workflow Efficiency**: Completing complex analyses in minutes instead of hours
5. **Decision Support**: Using forecasts for confident planning and risk management

### Interactive Examples

You can follow along with the video using the sample data provided in Lighthouse's demo mode. This allows hands-on practice with:

- Sample teams with realistic historical data
- Multiple projects in various states
- Features with different completion statuses
- Realistic flow metrics and forecasting scenarios

The demo environment provides a safe space to experiment with MCP integration before connecting to your production Lighthouse instance.

# Configuration Reference

This section provides complete configuration examples for all supported MCP clients and advanced configuration scenarios.

## Complete Client Configurations

### Claude Desktop (Complete Example)

**File Location**: 
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "globalShortcut": "Ctrl+Shift+K",
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "http://localhost:8080"
      ],
      "env": {
        "NODE_ENV": "production"
      }
    }
  }
}
```

### VS Code Copilot (Complete Example)

**File**: VS Code Settings JSON (`settings.json`)

```json
{
  "mcp": {
    "servers": {
      "lighthouse-mcp": {
        "type": "http",
        "url": "http://localhost:8080",
        "timeout": 30000,
        "retries": 3
      }
    }
  },
  "github.copilot.enable": {
    "*": true,
    "plaintext": false,
    "markdown": true
  }
}
```

### Continue Extension (Complete Example)

**File**: `.continue/config.json` in your workspace

```json
{
  "models": [
    {
      "title": "Claude 3.5 Sonnet",
      "provider": "anthropic",
      "model": "claude-3-5-sonnet-20241022",
      "apiKey": "your-api-key"
    }
  ],
  "mcpServers": {
    "lighthouse": {
      "transport": {
        "type": "http",
        "url": "http://localhost:8080"
      },
      "capabilities": {
        "tools": true,
        "prompts": true,
        "resources": true
      }
    }
  },
  "allowAnonymousTelemetry": false
}
```

### Cursor IDE (Complete Example)

**File**: Cursor Settings

```json
{
  "mcp": {
    "servers": {
      "lighthouse": {
        "command": "npx",
        "args": [
          "-y",
          "mcp-remote",
          "http://localhost:8080"
        ]
      }
    }
  }
}
```

## Advanced Configuration Scenarios

### Remote Lighthouse Instance with Authentication

```json
{
  "mcpServers": {
    "lighthouse-prod": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "https://lighthouse.yourcompany.com"
      ],
      "env": {
        "LIGHTHOUSE_API_KEY": "your-api-key",
        "NODE_TLS_REJECT_UNAUTHORIZED": "1"
      }
    }
  }
}
```

### Multiple Lighthouse Instances

```json
{
  "mcpServers": {
    "lighthouse-dev": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "http://localhost:8080"
      ]
    },
    "lighthouse-staging": {
      "command": "npx", 
      "args": [
        "mcp-remote",
        "https://lighthouse-staging.yourcompany.com"
      ]
    },
    "lighthouse-prod": {
      "command": "npx",
      "args": [
        "mcp-remote", 
        "https://lighthouse-prod.yourcompany.com"
      ]
    }
  }
}
```

### Custom Port Configuration

```json
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "http://localhost:5169"
      ]
    }
  }
}
```

### Corporate Network Configuration

```json
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "https://lighthouse.internal.company.com"
      ],
      "env": {
        "HTTP_PROXY": "http://proxy.company.com:8080",
        "HTTPS_PROXY": "http://proxy.company.com:8080",
        "NODE_TLS_REJECT_UNAUTHORIZED": "0"
      }
    }
  }
}
```

## Environment Variables Reference

### Lighthouse MCP Server Environment Variables

| Variable | Purpose | Default | Example |
|----------|---------|---------|---------|
| `LIGHTHOUSE_URL` | Override auto-detected URL | Auto-detected | `http://localhost:8080` |
| `MCP_TIMEOUT` | Request timeout in milliseconds | 30000 | `60000` |
| `MCP_DEBUG` | Enable debug logging | false | `true` |
| `NODE_TLS_REJECT_UNAUTHORIZED` | SSL certificate validation | 1 | `0` (disable for self-signed) |

### Client-Specific Environment Variables

**Claude Desktop**:
```json
{
  "env": {
    "NODE_ENV": "production",
    "DEBUG": "mcp:*"
  }
}
```

**Continue Extension**:
```json
{
  "env": {
    "CONTINUE_DEBUG": "true",
    "MCP_LOG_LEVEL": "debug"
  }
}
```

## Network Configuration

### Firewall Rules

For local Lighthouse instances, ensure these firewall rules:

**Windows Firewall**:
```cmd
netsh advfirewall firewall add rule name="Lighthouse MCP" dir=in action=allow protocol=TCP localport=8080
```

**macOS**:
```bash
sudo pfctl -f /etc/pf.conf
# Add to pf.conf: pass in proto tcp from any to any port 8080
```

**Linux (iptables)**:
```bash
sudo iptables -A INPUT -p tcp --dport 8080 -j ACCEPT
```

### Proxy Configuration

For corporate environments with proxy servers:

```json
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": ["mcp-remote", "https://lighthouse.company.com"],
      "env": {
        "HTTP_PROXY": "http://proxy.company.com:8080",
        "HTTPS_PROXY": "https://proxy.company.com:8080",
        "NO_PROXY": "localhost,127.0.0.1,.local"
      }
    }
  }
}
```

## Performance Tuning

### Timeout Configuration

```json
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": ["mcp-remote", "http://localhost:8080"],
      "env": {
        "MCP_TIMEOUT": "60000",
        "REQUEST_TIMEOUT": "45000"
      }
    }
  }
}
```

### Memory Optimization

```json
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "http://localhost:8080"
      ],
      "env": {
        "NODE_OPTIONS": "--max-old-space-size=2048"
      }
    }
  }
}
```

## Security Best Practices

### Production Environment

1. **Use HTTPS**: Always use HTTPS for remote instances
2. **API Keys**: Store API keys in environment variables, not config files
3. **Network Segmentation**: Restrict MCP server access to authorized networks
4. **Regular Updates**: Keep MCP client and server components updated

### Configuration File Security

```json
{
  "mcpServers": {
    "lighthouse": {
      "command": "npx",
      "args": ["mcp-remote", "https://lighthouse.company.com"],
      "env": {
        "LIGHTHOUSE_API_KEY": "${LIGHTHOUSE_API_KEY}",
        "NODE_TLS_REJECT_UNAUTHORIZED": "1"
      }
    }
  }
}
```

Set environment variable separately:
```bash
export LIGHTHOUSE_API_KEY="your-secret-key"
```

## Validation and Testing

### Configuration Validation

Test your configuration with these commands:

```bash
# Test Lighthouse accessibility
curl -I http://localhost:8080/health

# Test MCP endpoint
curl -I http://localhost:8080/mcp

# Validate JSON syntax
python -m json.tool < claude_desktop_config.json
```

### Connection Testing

1. **Basic Connectivity**: Verify Lighthouse loads in browser
2. **MCP Endpoint**: Check MCP server responds to requests
3. **Tool Execution**: Test a simple tool like `GetAllTeams`
4. **Error Handling**: Verify graceful error responses

This completes the comprehensive MCP configuration reference. For additional support, consult the troubleshooting section or reach out to the community.

---

# Document Summary

This comprehensive AI Integration guide provides everything needed to successfully implement and use the Lighthouse MCP server:

## ✅ Documentation Coverage Complete

**Configuration Examples**: Step-by-step setup for all popular MCP clients including Claude Desktop, VS Code Copilot, Continue, Cursor, and GitHub Copilot.

**Complete Tool Reference**: Detailed documentation of all 15+ tools across team management, project management, and feature management capabilities.

**Expert Prompts**: Professional-grade prompts for flow analysis, forecasting, and agile coaching based on industry best practices.

**Resources Access**: Full documentation resource catalog enabling AI assistants to provide contextual, accurate responses.

**Troubleshooting Guide**: Comprehensive solutions for connection issues, configuration problems, runtime errors, and client-specific issues.

**Real-World Scenarios**: Practical examples for Agile Coaches, Product Managers, Engineering Managers, and Executives.

**Security & Performance**: Best practices for production deployments and performance optimization.

## 🎥 Demo Video Plan

A comprehensive video demonstration is planned covering:
- Complete setup walkthrough
- Live tool demonstrations  
- Real-world scenario examples
- Troubleshooting common issues
- Best practices and tips

## 🚀 Getting Started

1. **Enable MCP**: Go to Lighthouse Settings > Optional Features > Enable "MCP Server"
2. **Choose Client**: Select your preferred AI assistant from the configuration examples
3. **Configure**: Follow the step-by-step setup instructions for your client
4. **Test**: Start with simple queries like "Show me all teams" or "Get Alpha team metrics"
5. **Explore**: Try the real-world scenarios provided in this guide

## 📞 Support

- **Community**: [Slack workspace](https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A)
- **Documentation**: [Full Lighthouse docs](https://docs.lighthouse.letpeople.work)
- **Issues**: [GitHub repository](https://github.com/LetPeopleWork/Lighthouse)

The Lighthouse MCP integration represents a significant advancement in AI-powered project management, bringing sophisticated analytics and expert guidance directly into your natural language workflow.
