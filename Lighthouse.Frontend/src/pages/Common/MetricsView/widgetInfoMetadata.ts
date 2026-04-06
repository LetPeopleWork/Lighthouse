const DOCS_BASE = "https://docs.lighthouse.letpeople.work/metrics/widgets.html";

export interface WidgetInfoEntry {
	readonly description: string;
	readonly learnMoreUrl: string;
}

export const widgetInfoMetadata: Record<string, WidgetInfoEntry> = {
	wipOverview: {
		description:
			"Current number of items in progress and how they compare to your WIP limit.",
		learnMoreUrl: `${DOCS_BASE}#work-items-in-progress`,
	},
	blockedOverview: {
		description:
			"Number of items currently blocked. The goal is always zero blocked items.",
		learnMoreUrl: `${DOCS_BASE}#work-items-in-progress`,
	},
	featuresWorkedOnOverview: {
		description:
			"How many features your team is actively working on compared to the Feature WIP limit.",
		learnMoreUrl: `${DOCS_BASE}#work-items-in-progress`,
	},
	predictabilityScore: {
		description:
			"A score showing how close together your forecast percentiles are — higher means more predictable.",
		learnMoreUrl: `${DOCS_BASE}#predictability-score`,
	},
	predictabilityScoreDetails: {
		description:
			"Detailed forecast distribution behind the predictability score.",
		learnMoreUrl: `${DOCS_BASE}#predictability-score`,
	},
	percentiles: {
		description:
			"Cycle time percentiles for the selected date range, compared to your Service Level Expectation.",
		learnMoreUrl: `${DOCS_BASE}#cycle-time-percentiles`,
	},
	startedVsFinished: {
		description:
			"Compares items started versus completed to show whether WIP is growing, shrinking, or stable.",
		learnMoreUrl: `${DOCS_BASE}#started-vs-closed`,
	},
	totalWorkItemAge: {
		description:
			"Sum of ages of all items currently in progress — a measure of your WIP inventory burden.",
		learnMoreUrl: `${DOCS_BASE}#total-work-item-age`,
	},
	throughput: {
		description:
			"Daily count of completed items over the selected range, shown as a run chart.",
		learnMoreUrl: `${DOCS_BASE}#throughput-run-chart`,
	},
	cycleScatter: {
		description:
			"Scatter plot of completed items showing cycle time trends and outliers over time.",
		learnMoreUrl: `${DOCS_BASE}#cycle-time-scatterplot`,
	},
	workDistribution: {
		description:
			"Breakdown of work items by their parent feature or epic to see where effort is concentrated.",
		learnMoreUrl: `${DOCS_BASE}#work-distribution`,
	},
	aging: {
		description:
			"In-progress items plotted by state and age to find items that may be stuck.",
		learnMoreUrl: `${DOCS_BASE}#work-item-aging-chart`,
	},
	wipOverTime: {
		description:
			"Historical trend of items in progress over the selected date range.",
		learnMoreUrl: `${DOCS_BASE}#wip-over-time`,
	},
	totalWorkItemAgeOverTime: {
		description:
			"Historical trend of the total age of all in-progress items over time.",
		learnMoreUrl: `${DOCS_BASE}#total-work-item-age`,
	},
	stacked: {
		description:
			"Simplified Cumulative Flow Diagram showing Doing and Done areas to reveal flow balance.",
		learnMoreUrl: `${DOCS_BASE}#simplified-cumulative-flow-diagram-cfd`,
	},
	estimationVsCycleTime: {
		description:
			"Scatter plot comparing estimates to actual cycle time to validate estimation accuracy.",
		learnMoreUrl: `${DOCS_BASE}#estimation-vs-cycle-time`,
	},
	featureSize: {
		description:
			"Feature sizes on a scatter plot, filterable by state, to spot size/cycle-time correlations.",
		learnMoreUrl: `${DOCS_BASE}#feature-size`,
	},
	throughputPbc: {
		description:
			"Process Behaviour Chart for throughput — highlights special-cause variation in delivery rate.",
		learnMoreUrl: `${DOCS_BASE}#throughput-process-behaviour-chart`,
	},
	wipPbc: {
		description:
			"Process Behaviour Chart for WIP — highlights special-cause variation in work in progress.",
		learnMoreUrl: `${DOCS_BASE}#work-in-progress-process-behaviour-chart`,
	},
	totalWorkItemAgePbc: {
		description:
			"Process Behaviour Chart for total work item age — highlights special-cause variation in inventory age.",
		learnMoreUrl: `${DOCS_BASE}#total-work-item-age-process-behaviour-chart`,
	},
	cycleTimePbc: {
		description:
			"Process Behaviour Chart for cycle time — highlights special-cause variation in delivery speed.",
		learnMoreUrl: `${DOCS_BASE}#cycle-time-process-behaviour-chart`,
	},
	featureSizePbc: {
		description:
			"Process Behaviour Chart for feature size — highlights special-cause variation in feature scope.",
		learnMoreUrl: `${DOCS_BASE}#feature-size-process-behaviour-chart`,
	},
};

export function getWidgetInfo(widgetKey: string): WidgetInfoEntry | undefined {
	return widgetInfoMetadata[widgetKey];
}
