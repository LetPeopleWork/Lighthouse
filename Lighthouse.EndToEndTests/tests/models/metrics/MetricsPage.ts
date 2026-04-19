import type { Locator, Page } from "@playwright/test";
import { WorkItemsDialog } from "./WorkItemsDialog";

export class MetricsWidget {
	page: Page;
	widgetId: string;
	name: string;
	constructor(page: Page, name: string, widgetId: string) {
		this.page = page;
		this.name = name;
		this.widgetId = widgetId;
	}

	async openDialog(): Promise<WorkItemsDialog> {
		await this.ViewDataButton.click();
		return new WorkItemsDialog(this.page);
	}

	get Id(): string {
		return this.widgetId;
	}

	get Name(): string {
		return this.name;
	}

	get ViewDataButton(): Locator {
		return this.page.getByTestId(`widget-view-data-${this.widgetId}`);
	}

	get InfoButton(): Locator {
		return this.page.getByTestId(`widget-info-${this.widgetId}`);
	}

	get Widget(): Locator {
		return this.page.locator(`[data-testid="dashboard-item-${this.widgetId}"]`);
	}
}

export enum MetricsCategories {
	FlowOverview = "flow-overview",
	FlowMetrics = "flow-metrics",
	Predictability = "predictability",
	PortfolioAndFeatures = "portfolio",
}

export const MetricsWidgetNames = {
	WorkInProgressOverview: "Work In Progress Overview",
	BlockedItemsOverview: "Blocked Items Overview",
	FeaturesBeingWorkedOnOverview: "Features Being Worked On Overview",
	TotalWorkItemAgeOverview: "Total Work Item Age Overview",
	PredictabilityScoreOverview: "Predictability Score Overview",
	CycleTimePercentiles: "Cycle Time Percentiles",
	CycleTimeScatterplot: "Cycle Time Scatterplot",
	WorkItemAgingChart: "Work Item Aging Chart",
	CycleTimeProcessBehaviourChart: "Cycle Time Process Behaviour Chart",
	ThroughputRunChart: "Throughput Run Chart",
	SimplifiedCumulativeFlowDiagram: "Simplified Cumulative Flow Diagram",
	ThroughputProcessBehaviourChart: "Throughput Process Behaviour Chart",
	WorkItemsInProgressOverTime: "Work Items In Progress Over Time",
	TotalWorkItemAgeOverTime: "Total Work Item Age Over Time",
	WorkInProgressProcessBehaviourChart:
		"Work In Progress Process Behaviour Chart",
	TotalWorkItemAgeProcessBehaviourChart:
		"Total Work Item Age Process Behaviour Chart",
	PredictabilityScoreDetails: "Predictability Score Details",
	WorkDistributionByFeature: "Work Distribution by Feature",
	FeatureSize: "Feature Size",
	EstimationVsCycleTime: "Estimation vs. Cycle Time",
	FeatureSizeProcessBehaviourChart: "Feature Size Process Behaviour Chart",
	FeatureSizePercentiles: "Feature Size Percentiles",
};

export class MetricsPage {
	page: Page;

	categoryWidgets: Record<MetricsCategories, [string, string][]> = {
		[MetricsCategories.FlowOverview]: [
			["Work In Progress Overview", "wipOverview"],
			["Blocked Items Overview", "blockedOverview"],
			["Features Being Worked On Overview", "featuresWorkedOnOverview"],
			["Total Work Item Age Overview", "totalWorkItemAge"],
			["Predictability Score Overview", "predictabilityScore"],
			["Cycle Time Percentiles", "percentiles"],
			["Total Throughput", "totalThroughput"],
			["Total Arrivals", "totalArrivals"],
			["Feature Size Percentiles", "featureSizePercentiles"],
		],
		[MetricsCategories.FlowMetrics]: [
			["Cycle Time Scatterplot", "cycleScatter"],
			["Work Item Aging Chart", "aging"],
			["Throughput Run Chart", "throughput"],
			["Simplified Cumulative Flow Diagram", "stacked"],
			["Work Items In Progress Over Time", "wipOverTime"],
			["Total Work Item Age Over Time", "totalWorkItemAgeOverTime"],
		],
		[MetricsCategories.Predictability]: [
			["Predictability Score Details", "predictabilityScoreDetails"],
			["Arrivals Run Chart", "arrivals"],
			["Throughput Process Behaviour Chart", "throughputPbc"],
			["Arrivals Process Behaviour Chart", "arrivalsPbc"],
			["Work In Progress Process Behaviour Chart", "wipPbc"],
			["Total Work Item Age Process Behaviour Chart", "totalWorkItemAgePbc"],
			["Cycle Time Process Behaviour Chart", "cycleTimePbc"],
			["Feature Size Process Behaviour Chart", "featureSizePbc"],
		],
		[MetricsCategories.PortfolioAndFeatures]: [
			["Work Distribution by Feature", "workDistribution"],
			["Feature Size", "featureSize"],
			["Estimation vs. Cycle Time", "estimationVsCycleTime"],
		],
	};

	constructor(page: Page, mode: "team" | "portfolio") {
		this.page = page;

		// Remove Features being worked on if we're on the portfolio Level
		if (mode === "portfolio") {
			this.categoryWidgets[MetricsCategories.PortfolioAndFeatures] =
				this.categoryWidgets[MetricsCategories.PortfolioAndFeatures].filter(
					([name]) => name !== MetricsWidgetNames.FeaturesBeingWorkedOnOverview,
				);
		}

		// Remove Feature Size and Feature Size Process Behaviour Chart from team level as they don't make sense without the context of a portfolio
		if (mode === "team") {
			this.categoryWidgets[MetricsCategories.Predictability] =
				this.categoryWidgets[MetricsCategories.Predictability].filter(
					([name]) =>
						name !== MetricsWidgetNames.FeatureSizeProcessBehaviourChart,
				);
			this.categoryWidgets[MetricsCategories.PortfolioAndFeatures] =
				this.categoryWidgets[MetricsCategories.PortfolioAndFeatures].filter(
					([name]) => name !== MetricsWidgetNames.FeatureSize,
				);
		}
	}

	async switchCategory(
		categoryName: MetricsCategories,
	): Promise<MetricsWidget[]> {
		await this.page.getByTestId(`category-chip-${categoryName}`).click();
		return this.getAvailableWidgets(categoryName);
	}

	getAvailableWidgets(category: MetricsCategories): MetricsWidget[] {
		return this.categoryWidgets[category].map(
			([name, widgetId]) => new MetricsWidget(this.page, name, widgetId),
		);
	}

	async getWidgetByName(
		name: string,
		availableWidgets: MetricsWidget[],
	): Promise<MetricsWidget> {
		const widget = availableWidgets.find((widget) => widget.Name === name);

		if (!widget) {
			throw new Error(`Widget with name ${name} not found`);
		}

		return widget;
	}
}
