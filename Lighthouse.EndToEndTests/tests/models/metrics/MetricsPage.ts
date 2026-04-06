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
	CycleTime = "cycle-time",
	Throughput = "throughput",
	WipAging = "wip-aging",
	Predictability = "predictability",
	PortfolioAndFeatures = "portfolio",
}

export const MetricsWidgetNames = {
	WorkInProgressOverview: "Work In Progress Overview",
	BlockedItemsOverview: "Blocked Items Overview",
	FeaturesBeingWorkedOnOverview: "Features Being Worked On Overview",
	TotalWorkItemAgeOverview: "Total Work Item Age Overview",
	PredictabilityScoreOverview: "Predictability Score Overview",
	StartedVsFinished: "Started vs. Finished",
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
			["Started vs. Finished", "startedVsFinished"],
			["Cycle Time Percentiles", "percentiles"],
		],
		[MetricsCategories.CycleTime]: [
			["Cycle Time Percentiles", "percentiles"],
			["Total Work Item Age Overview", "totalWorkItemAge"],
			["Cycle Time Scatterplot", "cycleScatter"],
			["Work Item Aging Chart", "aging"],
			["Cycle Time Process Behaviour Chart", "cycleTimePbc"],
		],
		[MetricsCategories.Throughput]: [
			["Started vs. Finished", "startedVsFinished"],
			["Predictability Score Overview", "predictabilityScore"],
			["Throughput Run Chart", "throughput"],
			["Simplified Cumulative Flow Diagram", "stacked"],
			["Throughput Process Behaviour Chart", "throughputPbc"],
		],
		[MetricsCategories.WipAging]: [
			["Work In Progress Overview", "wipOverview"],
			["Total Work Item Age Overview", "totalWorkItemAge"],
			["Work Item Aging Chart", "aging"],
			["Work Items In Progress Over Time", "wipOverTime"],
			["Total Work Item Age Over Time", "totalWorkItemAgeOverTime"],
			["Work In Progress Process Behaviour Chart", "wipPbc"],
			["Total Work Item Age Process Behaviour Chart", "totalWorkItemAgePbc"],
		],
		[MetricsCategories.Predictability]: [
			["Cycle Time Percentiles", "percentiles"],
			["Predictability Score Details", "predictabilityScoreDetails"],
			["Throughput Process Behaviour Chart", "throughputPbc"],
			["Work In Progress Process Behaviour Chart", "wipPbc"],
			["Total Work Item Age Process Behaviour Chart", "totalWorkItemAgePbc"],
			["Cycle Time Process Behaviour Chart", "cycleTimePbc"],
			["Feature Size Process Behaviour Chart", "featureSizePbc"],
		],
		[MetricsCategories.PortfolioAndFeatures]: [
			["Features Being Worked On Overview", "featuresWorkedOnOverview"],
			["Predictability Score Overview", "predictabilityScore"],
			["Work Distribution by Feature", "workDistribution"],
			["Feature Size", "featureSize"],
			["Estimation vs. Cycle Time", "estimationVsCycleTime"],
		],
	};

	constructor(page: Page) {
		this.page = page;
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

	async getWidgetByName(name: string): Promise<MetricsWidget> {
		const widgetId = Object.values(MetricsWidgetNames).find(
			(key) => key === name,
		);

		if (!widgetId) {
			throw new Error(`Widget with name ${name} not found`);
		}

		return new MetricsWidget(this.page, name, widgetId);
	}
}
