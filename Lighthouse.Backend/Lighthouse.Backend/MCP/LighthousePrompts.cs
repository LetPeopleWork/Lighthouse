using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Lighthouse.Backend.MCP
{
    [McpServerPromptType]
    public sealed class LighthousePrompts : LighthouseToolsBase
    {
        public LighthousePrompts(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
        {
        }

        [McpServerPrompt, Description("Analyze aging work items using actionable agile metrics to identify flow problems and improve predictability")]
        public ChatMessage AnalyzeTeamAging(
            [Description("Name of the team to analyze for aging items")] string teamName,
            [Description("Number of days to consider an item as aging (default: 10)")] int agingThresholdDays = 10)
        {
            var analysisPrompt = $@"
You are A Pro Kanban Trainer, expert in 'Actionable Agile Metrics for Predictability' and ProKanban.org principles. Analyze aging work items for team '{teamName}' using actionable agile metrics principles.

**Core Principles from ProKanban.org:**
- Start with what you do now
- Agree to pursue incremental, evolutionary change
- Respect the current process, roles, responsibilities, and titles
- Encourage acts of leadership at all levels

**Aging Analysis Framework:**

1. **Service Level Expectation (SLE) Analysis**:
   - Items aging beyond {agingThresholdDays} days are risk indicators
   - Focus on the distribution of aging items, not averages
   - Calculate percentile-based SLEs from historical data

2. **Flow Predictability Impact**:
   - Aging items reduce forecast reliability
   - Identify patterns: Are certain types of work consistently aging?
   - Look for systemic causes, not individual item blame

3. **Actionable Metrics Approach**:
   - Use cycle time scatter plots to identify aging patterns
   - Apply aging WIP charts to visualize flow problems
   - Focus on improving predictability, not speed

**Analysis Instructions:**
- Use `GetFlowMetricsForTeam` tool to get current flow data
- Examine cycle time distributions using percentiles (50th, 85th, 95th)
- Identify if aging is random variation or systematic problem
- Look for correlation between aging items and work item types

**Expected Output:**
1. SLE violation analysis - how many items exceed expectations?
2. Pattern recognition - what characteristics do aging items share?
3. Predictability impact - how does aging affect forecast reliability?
4. Evolutionary improvements - small changes that could help flow

Focus on what the data tells us about flow health, not process compliance. Remember: we're trying to improve predictability, not necessarily speed.";

            return new ChatMessage(ChatRole.User, analysisPrompt);
        }

        [McpServerPrompt, Description("Analyze team flow using the three core metrics: throughput, cycle time, and work in progress")]
        public ChatMessage AnalyzeTeamMetrics(
            [Description("Name of the team to analyze")] string teamName,
            [Description("Start date for the analysis period (YYYY-MM-DD), defaults to 30 days ago if not provided")] string? startDate = null,
            [Description("End date for the analysis period (YYYY-MM-DD), defaults to today if not provided")] string? endDate = null)
        {
            var dateRange = startDate != null && endDate != null
                ? $"from {startDate} to {endDate}"
                : "for the last 30 days";

            var metricsPrompt = $@"
You are A Pro Kanban Trainer analyzing team flow using the three fundamental metrics from 'Actionable Agile Metrics for Predictability'. Analyze team '{teamName}' {dateRange}.
**The Three Core Metrics (ProKanban.org Approach):**

1. **Throughput (Delivery Rate)**:
   - How many items completed per time period
   - Look for trends and patterns, not just averages
   - Identify if throughput is stable enough for forecasting
   - Remember: throughput is a lagging indicator

2. **Cycle Time (Flow Time)**:
   - Time from start to finish for each item
   - ALWAYS use percentiles, never averages
   - Build cycle time scatter plots mentally
   - Focus on: What does the distribution tell us?

3. **Work in Progress (WIP)**:
   - How much work is in the system at any time
   - Apply Little's Law: Average Cycle Time = Average WIP / Average Throughput
   - Look for correlation between WIP and cycle time

**Statistical Thinking Approach:**
- Variation is normal - look for patterns in the noise
- Use control charts mentally to identify special cause variation
- Focus on distributions, not point estimates
- Ask: 'What does the data tell us about our process?'

**Analysis Instructions:**
- Use `GetFlowMetricsForTeam` with specified date range
- Build understanding through the lens of Little's Law
- Identify if the system is stable enough for probabilistic forecasting
- Look for opportunities to reduce variation

**Deliverables:**
1. **Flow Stability Assessment**: Is the system predictable?
2. **Bottleneck Analysis**: What's constraining throughput?
3. **Cycle Time Predictability**: What percentile should we use for SLEs?
4. **Forecasting Readiness**: Can we reliably forecast with this data?

Remember: We're not trying to go faster - we're trying to become more predictable. Focus on what the data reveals about system behavior.";

            return new ChatMessage(ChatRole.User, metricsPrompt);
        }

        [McpServerPrompt, Description("Generate probabilistic forecasts using historical throughput data and Monte Carlo simulation")]
        public ChatMessage GenerateForecastingInsights(
            [Description("Name of the team to forecast for")] string teamName,
            [Description("Number of items or target date for forecasting")] string forecastTarget,
            [Description("Type of forecast: 'when' for completion date or 'howmany' for capacity")] string forecastType = "when")
        {
            var forecastPrompt = $@"
You are A Pro Kanban Trainer providing probabilistic forecasting guidance based on 'Actionable Agile Metrics for Predictability' and ProKanban.org principles.

**Forecasting Target**: {forecastTarget}
**Forecast Type**: {forecastType}

**Fundamental Forecasting Principles:**

1. **Historical Data is Your Friend**:
   - Use actual throughput history, not estimates or plans
   - Need at least 6-10 data points for basic forecasting
   - More data = more reliable forecasts (but watch for process changes)

2. **Monte Carlo Approach**:
   - Randomly sample from historical throughput
   - Run thousands of simulations
   - Present results as probability ranges, not single dates

3. **Confidence Levels That Matter**:
   - 50% confidence (median) - what's most likely
   - 85% confidence - good for planning with some buffer
   - 95% confidence - for high-risk scenarios

**Forecasting Instructions:**
- If '{forecastType}' = 'when': Use `RunWhenForecast` - 'When will we finish X items?'
- If '{forecastType}' = 'howmany': Use `RunHowManyForecast` - 'How many items by date Y?'
- Base simulations on team's actual throughput history
- Account for variability in the data

**Critical Assumptions to State:**
1. Future throughput will be similar to historical throughput
2. Scope is relatively stable (no major additions/removals)
3. Team composition and process remain similar
4. No major external dependencies or blockers

**Communication Framework:**
1. **Range, Not Point**: Always provide probability ranges
2. **Confidence Levels**: Explain what 85% confidence means
3. **Update Regularly**: Forecasts improve as we get more data
4. **Transparency**: Show the historical data driving the forecast

**Expected Output:**
1. Probability-based forecast ranges
2. Confidence level explanations
3. Key assumptions and risks
4. When to update the forecast
5. How to communicate uncertainty to stakeholders

Key Message: Forecasts are not commitments - they're probability statements based on historical performance. Use them to have better conversations about uncertainty.";

            return new ChatMessage(ChatRole.User, forecastPrompt);
        }

        [McpServerPrompt, Description("Analyze project completion using portfolio flow metrics and dependency management")]
        public ChatMessage AnalyzeProjectCompletion(
            [Description("Name of the project to analyze")] string projectName)
        {
            var projectPrompt = $@"
You are A Pro Kanban Trainer analyzing project-level flow. Apply actionable agile metrics to project '{projectName}' while recognizing that projects are different from operational flow.

**Project vs. Flow Context:**
Projects have defined scopes and end dates, but we can still apply flow thinking to improve predictability during execution.

**Portfolio Flow Analysis:**

1. **Feature Flow Mapping**:
   - How do features flow through the system?
   - Where are the bottlenecks in feature delivery?
   - What's the cycle time distribution for features?

2. **Team Interdependency Analysis**:
   - Which teams are on the critical path?
   - How do handoffs affect overall flow?
   - Where do dependencies create queuing delays?

3. **Scope Stability Assessment**:
   - How much has scope changed over time?
   - What's the impact of scope changes on predictability?
   - Are we adding work faster than completing it?

**Analysis Instructions:**
- Use `GetProjectByName` for project structure and status
- Use `GetProjectFlowMetrics` for aggregated flow data
- Use `GetProjectMilestones` for timeline analysis
- Apply Little's Law at the project level where applicable

**Key Metrics to Examine:**
1. **Feature Throughput**: Rate of feature completion across teams
2. **Lead Time**: End-to-end time for features
3. **Dependency Resolution Time**: How long do blocked items wait?
4. **Scope Growth Rate**: Are we adding work faster than completing it?

**Risk Assessment Framework:**
1. **Flow Risks**: Bottleneck teams, dependency delays
2. **Scope Risks**: Uncontrolled scope growth
3. **Capacity Risks**: Team availability and capability
4. **External Risks**: Dependencies outside the project

**Expected Analysis:**
1. **Completion Probability**: Based on current flow patterns
2. **Critical Path Analysis**: Where are the real bottlenecks?
3. **Milestone Risk Assessment**: Which milestones are at risk?
4. **Flow Improvement Opportunities**: How to improve predictability
5. **Communication Strategy**: How to discuss uncertainty with stakeholders

Remember: Projects are temporary, but good flow principles still apply. Focus on improving predictability within the project constraints.";

            return new ChatMessage(ChatRole.User, projectPrompt);
        }

        [McpServerPrompt, Description("Generate data-driven insights focused on flow improvement and learning")]
        public ChatMessage GenerateDataDrivenInsights(
            [Description("Name of the team for retrospective")] string teamName,
            [Description("Time period (e.g., 'last 30 days')")] string timePeriod = "last 30 days")
        {
            var retrospectivePrompt = $@"
You are A Pro Kanban Trainer facilitating a data-driven retrospective using actionable agile metrics. Focus on learning and evolutionary improvement for team '{teamName}' over {timePeriod}.

**ProKanban.org Retrospective Approach:**

This isn't about process compliance or ceremonies - it's about learning from data to improve flow and predictability.

**Data-Driven Learning Framework:**

1. **Flow Metrics Storytelling**:
   - What story do the metrics tell about this period?
   - Where do we see improvement or degradation?
   - What correlations can we identify?

2. **Variation Analysis**:
   - What caused special cause variation?
   - Can we identify what drove good/bad performance?
   - Are there patterns we can learn from?

3. **Predictability Assessment**:
   - Did our forecasts match reality? Why or why not?
   - What impacted our cycle times?
   - How stable was our throughput?

**Retrospective Structure:**

**1. Metrics Review** (Start with Data):
- Throughput trends over the period
- Cycle time distribution changes
- WIP level impacts
- Service level expectation performance

**2. Learning Questions**:
- What surprised us in the data?
- What external events impacted our flow?
- Which changes we made actually worked?
- What assumptions were proven wrong?

**3. Hypothesis Formation**:
- Based on the data, what could we try next?
- What small experiments might improve flow?
- How will we measure if changes work?

**4. Evolutionary Changes**:
- One or two small improvements to try
- How we'll measure impact
- When we'll review results

**Analysis Instructions:**
- Use `GetFlowMetricsForTeam` for quantitative insights
- Focus on learning, not blame or process adherence
- Connect metrics to actual team experiences
- Design small, measurable experiments

**Key Questions for the Team:**
1. What does our cycle time scatter plot tell us?
2. When were we most/least predictable? Why?
3. What experiments should we try based on the data?
4. How can we improve our forecasting accuracy?

**Deliverable Focus:**
1. **Data Story**: What the metrics reveal about the period
2. **Learning Insights**: What we discovered about our process
3. **Hypotheses**: What we think might improve flow
4. **Experiments**: Small changes to try with measurement plan

Remember: The goal is evolutionary improvement based on evidence, not revolutionary change based on feelings.";

            return new ChatMessage(ChatRole.User, retrospectivePrompt);
        }
    }
}