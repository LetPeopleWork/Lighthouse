const DOCS_BASE = "https://docs.lighthouse.letpeople.work/metrics/widgets.html";

export interface WidgetStatusGuidance {
	readonly sustain: string;
	readonly observe: string;
	readonly act: string;
}

export interface WidgetInfoEntry {
	readonly description: string;
	readonly learnMoreUrl: string;
	readonly statusGuidance: WidgetStatusGuidance;
}

export const widgetInfoMetadata: Record<string, WidgetInfoEntry> = {
	wipOverview: {
		description:
			"Current number of items in progress and how they compare to your WIP limit.",
		learnMoreUrl: `${DOCS_BASE}#work-items-in-progress`,
		statusGuidance: {
			sustain: "Current WIP exactly matches the System WIP Limit.",
			observe: "Current WIP is below the System WIP Limit.",
			act: "No System WIP Limit is configured, or current WIP exceeds the limit.",
		},
	},
	blockedOverview: {
		description:
			"Number of items currently blocked. The goal is always zero blocked items.",
		learnMoreUrl: `${DOCS_BASE}#work-items-in-progress`,
		statusGuidance: {
			sustain: "No items are blocked.",
			observe: "Exactly 1 item is blocked.",
			act: "No blocked indicators are configured, or 2 or more items are blocked.",
		},
	},
	featuresWorkedOnOverview: {
		description:
			"How many features your team is actively working on compared to the Feature WIP limit.",
		learnMoreUrl: `${DOCS_BASE}#work-items-in-progress`,
		statusGuidance: {
			sustain: "Feature count exactly matches the Feature WIP limit.",
			observe: "Fewer features are being worked on than the Feature WIP limit.",
			act: "No Feature WIP is configured, or active feature count exceeds the limit.",
		},
	},
	predictabilityScore: {
		description:
			"A score showing how close together your forecast percentiles are — higher means more predictable.",
		learnMoreUrl: `${DOCS_BASE}#predictability-score`,
		statusGuidance: {
			sustain: "Score is above 60%, so forecasts are considered trustworthy.",
			observe:
				"Score is between 40% and 60%; investigate patterns that reduce stability.",
			act: "Score is below 40%, indicating highly variable throughput.",
		},
	},
	predictabilityScoreDetails: {
		description:
			"Detailed forecast distribution behind the predictability score.",
		learnMoreUrl: `${DOCS_BASE}#predictability-score`,
		statusGuidance: {
			sustain: "Score is above 60%, so forecasts are considered trustworthy.",
			observe:
				"Score is between 40% and 60%; investigate patterns that reduce stability.",
			act: "Score is below 40%, indicating highly variable throughput.",
		},
	},
	percentiles: {
		description:
			"Cycle time percentiles for the selected date range, compared to your Service Level Expectation.",
		learnMoreUrl: `${DOCS_BASE}#cycle-time-percentiles`,
		statusGuidance: {
			sustain:
				"The percentage of items within SLE meets or exceeds the configured target percentile.",
			observe:
				"The percentage of items within SLE is below target by up to 20 percentage points.",
			act: "No SLE is configured, no closed items exist in range, or within-SLE performance is more than 20 percentage points below target.",
		},
	},
	startedVsFinished: {
		description:
			"Compares items started versus completed to show whether WIP is growing, shrinking, or stable.",
		learnMoreUrl: `${DOCS_BASE}#started-vs-closed`,
		statusGuidance: {
			sustain:
				"Started and closed are balanced (both 0, difference under 2, or within 5%).",
			observe:
				"Closed significantly exceeds started, which may indicate starving the process.",
			act: "No System WIP Limit is configured, or started exceeds closed by more than 5%.",
		},
	},
	totalWorkItemAge: {
		description:
			"Sum of ages of all items currently in progress — a measure of your WIP inventory burden.",
		learnMoreUrl: `${DOCS_BASE}#total-work-item-age`,
		statusGuidance: {
			sustain:
				"Total age is within the reference value and not projected to exceed it tomorrow.",
			observe:
				"Total age is currently within the reference value, but tomorrow's projection would exceed it.",
			act: "System WIP Limit or SLE is not configured, or current total age already exceeds the reference value.",
		},
	},
	throughput: {
		description:
			"Daily count of completed items over the selected range, shown as a run chart.",
		learnMoreUrl: `${DOCS_BASE}#throughput-run-chart`,
		statusGuidance: {
			sustain: "No extended zero-throughput runs are detected.",
			observe:
				"Exactly 1 run of 3 consecutive zero-throughput days is detected.",
			act: "2 or more runs of 3+ consecutive zero-throughput days are detected.",
		},
	},
	cycleScatter: {
		description:
			"Scatter plot of completed items showing cycle time trends and outliers over time.",
		learnMoreUrl: `${DOCS_BASE}#cycle-time-scatterplot`,
		statusGuidance: {
			sustain:
				"The percentage of items exceeding SLE is within the expected threshold.",
			observe:
				"The percentage of items exceeding SLE is above the allowed threshold, but within 10 percentage points.",
			act: "No SLE is configured, or the percentage of items exceeding SLE is more than 10 percentage points above the allowed threshold.",
		},
	},
	workDistribution: {
		description:
			"Breakdown of work items by their parent feature or epic to see where effort is concentrated.",
		learnMoreUrl: `${DOCS_BASE}#work-distribution`,
		statusGuidance: {
			sustain:
				"Work is spread within the Feature WIP limit and fewer than 20% of items are unlinked.",
			observe:
				"Work is spread slightly above the Feature WIP limit (up to 120%), or unlinked items are below 20%.",
			act: "20% or more items are unlinked, Feature WIP is not configured, or work is spread across more than 120% of the Feature WIP limit.",
		},
	},
	aging: {
		description:
			"In-progress items plotted by state and age to find items that may be stuck.",
		learnMoreUrl: `${DOCS_BASE}#work-item-aging-chart`,
		statusGuidance: {
			sustain: "All in-progress items are within SLE and none are blocked.",
			observe:
				"Some items exceed SLE or at least one item is blocked, but not both together.",
			act: "No SLE or blocked indicators are configured, or too many items exceed SLE and at least one item is blocked.",
		},
	},
	wipOverTime: {
		description:
			"Historical trend of items in progress over the selected date range.",
		learnMoreUrl: `${DOCS_BASE}#wip-over-time`,
		statusGuidance: {
			sustain:
				"WIP is exactly at the System WIP Limit on more than 50% of days.",
			observe:
				"WIP is mostly below the limit, or day distribution across above/at/below is uneven.",
			act: "No System WIP Limit is configured, or WIP exceeded the limit on more days than it was at or below it.",
		},
	},
	totalWorkItemAgeOverTime: {
		description:
			"Historical trend of the total age of all in-progress items over time.",
		learnMoreUrl: `${DOCS_BASE}#total-work-item-age`,
		statusGuidance: {
			sustain:
				"Total age is stable (within ±10%) or remains 0 throughout the period.",
			observe:
				"Total age dropped by more than 10%; verify whether this reflects removals or a completion burst.",
			act: "Total age grew from 0 to a positive value, or increased by more than 10% over the period.",
		},
	},
	stacked: {
		description:
			"Simplified Cumulative Flow Diagram showing Doing and Done areas to reveal flow balance.",
		learnMoreUrl: `${DOCS_BASE}#simplified-cumulative-flow-diagram-cfd`,
		statusGuidance: {
			sustain:
				"Started and closed are balanced (within 5% or an absolute difference below 2).",
			observe:
				"Closed significantly exceeds started, which may indicate process starvation.",
			act: "No System WIP Limit is configured, or started exceeds closed by more than 5%.",
		},
	},
	estimationVsCycleTime: {
		description:
			"Scatter plot comparing estimates to actual cycle time to validate estimation accuracy.",
		learnMoreUrl: `${DOCS_BASE}#estimation-vs-cycle-time`,
		statusGuidance: {
			sustain: "Spearman correlation is 0.6 or above.",
			observe: "Spearman correlation is between 0.3 and 0.6.",
			act: "Estimation is not configured, or Spearman correlation is below 0.3.",
		},
	},
	featureSize: {
		description:
			"Feature sizes on a scatter plot, filterable by state, to spot size/cycle-time correlations.",
		learnMoreUrl: `${DOCS_BASE}#feature-size`,
		statusGuidance: {
			sustain:
				"Active features above the configured percentile size threshold stay within the expected percentage.",
			observe:
				"Active features above the percentile size threshold are slightly above the expected percentage.",
			act: "No Feature Size Target is configured, or active features above the threshold exceed the expected percentage by more than 10 points.",
		},
	},
	throughputPbc: {
		description:
			"Process Behaviour Chart for throughput — highlights special-cause variation in delivery rate.",
		learnMoreUrl: `${DOCS_BASE}#throughput-process-behaviour-chart`,
		statusGuidance: {
			sustain:
				"A baseline is configured and no special-cause signals are detected.",
			observe: "A Moderate Change signal is detected (without Large Change).",
			act: "No baseline is configured, or a Large Change signal is detected.",
		},
	},
	wipPbc: {
		description:
			"Process Behaviour Chart for WIP — highlights special-cause variation in work in progress.",
		learnMoreUrl: `${DOCS_BASE}#work-in-progress-process-behaviour-chart`,
		statusGuidance: {
			sustain:
				"A baseline is configured and no special-cause signals are detected.",
			observe: "A Moderate Change signal is detected (without Large Change).",
			act: "No baseline is configured, or a Large Change signal is detected.",
		},
	},
	totalWorkItemAgePbc: {
		description:
			"Process Behaviour Chart for total work item age — highlights special-cause variation in inventory age.",
		learnMoreUrl: `${DOCS_BASE}#total-work-item-age-process-behaviour-chart`,
		statusGuidance: {
			sustain:
				"A baseline is configured and no special-cause signals are detected.",
			observe: "A Moderate Change signal is detected (without Large Change).",
			act: "No baseline is configured, or a Large Change signal is detected.",
		},
	},
	cycleTimePbc: {
		description:
			"Process Behaviour Chart for cycle time — highlights special-cause variation in delivery speed.",
		learnMoreUrl: `${DOCS_BASE}#cycle-time-process-behaviour-chart`,
		statusGuidance: {
			sustain:
				"A baseline is configured and no special-cause signals are detected.",
			observe: "A Moderate Change signal is detected (without Large Change).",
			act: "No baseline is configured, or a Large Change signal is detected.",
		},
	},
	featureSizePbc: {
		description:
			"Process Behaviour Chart for feature size — highlights special-cause variation in feature scope.",
		learnMoreUrl: `${DOCS_BASE}#feature-size-process-behaviour-chart`,
		statusGuidance: {
			sustain:
				"A baseline is configured and no special-cause signals are detected.",
			observe: "A Moderate Change signal is detected (without Large Change).",
			act: "No baseline is configured, or a Large Change signal is detected.",
		},
	},
	arrivals: {
		description:
			"Daily count of items started (arrivals) over the selected range, shown as a run chart.",
		learnMoreUrl: `${DOCS_BASE}#arrivals-run-chart`,
		statusGuidance: {
			sustain:
				"Arrivals are balanced with departures and no significant batching is detected.",
			observe:
				"Arrivals are balanced overall, but noticeable batching suggests work is starting in bursts rather than continuously.",
			act: "No System WIP Limit is configured, or arrivals materially exceed departures.",
		},
	},
	arrivalsPbc: {
		description:
			"Process Behaviour Chart for arrivals — highlights special-cause variation in intake rate.",
		learnMoreUrl: `${DOCS_BASE}#arrivals-process-behaviour-chart`,
		statusGuidance: {
			sustain:
				"A baseline is configured and no special-cause signals are detected.",
			observe: "A Moderate Change signal is detected (without Large Change).",
			act: "No baseline is configured, or a Large Change signal is detected.",
		},
	},
};

export function getWidgetInfo(widgetKey: string): WidgetInfoEntry | undefined {
	return widgetInfoMetadata[widgetKey];
}
