import { expect, type Locator, type Page } from "@playwright/test";
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

	get forecastFilterToggle(): Locator {
		return this.Widget.getByLabel(/^Use filtered/i);
	}

	async toggleForecastFilter(): Promise<void> {
		await this.forecastFilterToggle.click();
	}

	async isForecastFilterEnabled(): Promise<boolean> {
		return await this.forecastFilterToggle.isChecked();
	}

	async snapshotChartContent(): Promise<string> {
		return (await this.Widget.innerText()) ?? "";
	}

	get staleOverviewCount(): Locator {
		return this.Widget.getByTestId("stale-overview-count");
	}

	async getStaleOverviewCount(): Promise<number> {
		const text = (await this.staleOverviewCount.innerText()) ?? "0";
		return Number(text.replace(/\D/g, ""));
	}

	get blockedOverviewCount(): Locator {
		return this.Widget.getByTestId("blocked-overview-count");
	}

	async getBlockedOverviewCount(): Promise<number> {
		const text = (await this.blockedOverviewCount.innerText()) ?? "0";
		return Number(text.replace(/\D/g, ""));
	}

	get ragStatusIndicator(): Locator {
		return this.Widget.getByTestId("rag-status");
	}

	async getRagStatus(): Promise<string> {
		return (await this.ragStatusIndicator.getAttribute("data-rag")) ?? "";
	}

	get staleAgingBubbles(): Locator {
		return this.Widget.getByTestId("aging-bubble-stale");
	}

	async countStaleAgingBubbles(): Promise<number> {
		return this.staleAgingBubbles.count();
	}

	async openDialogFromStaleBubble(): Promise<WorkItemsDialog> {
		await this.staleAgingBubbles.first().click();
		return new WorkItemsDialog(this.page);
	}

	// The blocked-over-time chart is a MUI-X BarChart; each column snapshot is a
	// <rect class="MuiBarChart-element">. The rightmost bar is the most recent
	// (today) snapshot.
	get blockedOverTimeBars(): Locator {
		return this.Widget.locator("rect.MuiBarChart-element");
	}

	async countBlockedOverTimeBars(): Promise<number> {
		return this.blockedOverTimeBars.count();
	}

	get blockedItemsDialog(): Locator {
		return this.page.getByRole("dialog");
	}

	get blockedItemsDialogRows(): Locator {
		return this.blockedItemsDialog.locator(".MuiDataGrid-row");
	}

	async countBlockedItemsDialogRows(): Promise<number> {
		return this.blockedItemsDialogRows.count();
	}

	/**
	 * Clicks the latest (rightmost) bar on the Blocked Items Over Time chart,
	 * which drills into the items blocked at that date (08-04 endpoint) and
	 * lists them in the shared WorkItemsDialog.
	 */
	async openBlockedOverTimeDialogByLatestBar(): Promise<WorkItemsDialog> {
		await this.blockedOverTimeBars.last().click();
		await this.blockedItemsDialog.waitFor({ state: "visible" });
		return new WorkItemsDialog(this.page);
	}
}

export class WorkItemAgePercentilesCard {
	private readonly card: Locator;

	constructor(public readonly page: Page) {
		this.card = page.locator(
			'[data-testid="dashboard-item-workItemAgePercentiles"]',
		);
	}

	get widget(): Locator {
		return this.card;
	}

	get title(): Locator {
		return this.card.getByText("Work Item Age Percentiles", { exact: true });
	}

	get percentileValues(): Locator {
		return this.card.getByText(/\d+ days?$/);
	}

	async countPercentileValues(): Promise<number> {
		return this.percentileValues.count();
	}
}

export class WorkItemAgingReferenceLineSelector {
	private readonly widget: Locator;

	constructor(
		public readonly page: Page,
		widgetId: string,
	) {
		this.widget = page.locator(`[data-testid="dashboard-item-${widgetId}"]`);
	}

	get chart(): Locator {
		return this.widget;
	}

	private toggle(name: string): Locator {
		return this.widget.getByRole("button", { name, exact: true });
	}

	get cycleTimeToggle(): Locator {
		return this.toggle("Cycle Time");
	}

	get workItemAgeToggle(): Locator {
		return this.toggle("Work Item Age");
	}

	private referenceLineLabels(pattern: RegExp): Locator {
		return this.widget.locator("text").filter({ hasText: pattern });
	}

	get cycleTimeReferenceLines(): Locator {
		return this.referenceLineLabels(/^\d+%$/);
	}

	get workItemAgeReferenceLines(): Locator {
		return this.referenceLineLabels(/^Work Item Age \d+%$/);
	}

	async selectCycleTime(): Promise<void> {
		await this.cycleTimeToggle.click();
	}

	async selectWorkItemAge(): Promise<void> {
		await this.workItemAgeToggle.click();
	}

	async countCycleTimeReferenceLines(): Promise<number> {
		return this.cycleTimeReferenceLines.count();
	}

	async countWorkItemAgeReferenceLines(): Promise<number> {
		return this.workItemAgeReferenceLines.count();
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
	FlowEfficiencyOverview: "Flow Efficiency Overview",
	BlockedItemsOverview: "Blocked Items Overview",
	StaleItemsOverview: "Stale Items Overview",
	FeaturesBeingWorkedOnOverview: "Features Being Worked On Overview",
	TotalWorkItemAgeOverview: "Total Work Item Age Overview",
	PredictabilityScoreOverview: "Predictability Score Overview",
	CycleTimePercentiles: "Cycle Time Percentiles",
	WorkItemAgePercentiles: "Work Item Age Percentiles",
	CycleTimeScatterplot: "Cycle Time Scatterplot",
	WorkItemAgingChart: "Work Item Aging Chart",
	CumulativeStateTime: "Cumulative Time per State",
	LoadBalanceMatrix: "Load Balance Matrix",
	CycleTimeProcessBehaviourChart: "Cycle Time Process Behaviour Chart",
	ThroughputRunChart: "Throughput Run Chart",
	SimplifiedCumulativeFlowDiagram: "Simplified Cumulative Flow Diagram",
	ThroughputProcessBehaviourChart: "Throughput Process Behaviour Chart",
	WorkItemsInProgressOverTime: "Work Items In Progress Over Time",
	TotalWorkItemAgeOverTime: "Total Work Item Age Over Time",
	BlockedItemsOverTime: "Blocked Items Over Time",
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

/**
 * Drives the rule-based Blocked Items configuration in the shared Flow Metrics
 * Configuration group (team + portfolio settings). The blocked rule set reuses
 * the shared DeliveryRuleBuilder (third consumer, mirroring ForecastFilterEditor),
 * so the rule-row selectors match the builder's data-testids exactly.
 */
export class BlockedRuleConfigEditor {
	constructor(public readonly page: Page) {}

	get flowMetricsConfigurationHeader(): Locator {
		return this.page.getByText("Flow Metrics Configuration", { exact: true });
	}

	get enableCheckbox(): Locator {
		return this.page.getByLabel("Configure Blocked Work Items");
	}

	get builder(): Locator {
		// The settings page hosts two shared DeliveryRuleBuilder instances
		// (Forecast Filter + Blocked). Scope to the blocked one by its title.
		return this.page
			.getByTestId("delivery-rule-builder")
			.filter({ hasText: "as blocked where" });
	}

	get addRuleButton(): Locator {
		return this.builder.getByTestId("add-rule-button");
	}

	get ruleRows(): Locator {
		return this.builder.getByTestId("rule-row");
	}

	/**
	 * Removes any pre-existing rule rows so a freshly added rule is the sole
	 * blocked definition — makes the resulting blocked count fully attributable
	 * to the rule this test saves.
	 */
	async clearExistingRules(): Promise<void> {
		await expect
			.poll(
				async () => {
					const count = await this.ruleRows.count();
					if (count === 0) {
						return 0;
					}
					await this.builder
						.getByTestId("rule-delete-0")
						.click()
						.catch(() => {});
					return this.ruleRows.count();
				},
				{ timeout: 15_000 },
			)
			.toBe(0);
	}

	async enable(): Promise<void> {
		await expect
			.poll(
				async () => {
					if (!(await this.enableCheckbox.isVisible())) {
						await this.flowMetricsConfigurationHeader.click().catch(() => {});
						return false;
					}
					if (!(await this.enableCheckbox.isChecked())) {
						await this.enableCheckbox.check().catch(() => {});
					}
					return this.enableCheckbox.isChecked();
				},
				{ timeout: 15_000 },
			)
			.toBe(true);

		// The builder only renders once the config admin's rule schema resolves.
		await expect(this.builder).toBeVisible({ timeout: 15_000 });
	}

	async addFieldEqualsRule(
		fieldDisplayName: string,
		value: string,
	): Promise<void> {
		await this.addRuleButton.click();

		const ruleIndex = (await this.ruleRows.count()) - 1;

		await this.chooseFromSelect(
			this.builder.getByTestId(`rule-field-select-${ruleIndex}`),
			fieldDisplayName,
		);
		await this.chooseFromSelect(
			this.builder.getByTestId(`rule-operator-select-${ruleIndex}`),
			"Equals",
		);

		const valueInput = this.builder
			.getByTestId(`rule-value-input-${ruleIndex}`)
			.locator("input");

		// The settings form auto-saves on a debounce, and the "saved" indicator does
		// not flip to "saving" until the debounce fires — so a caller that navigates
		// as soon as it reads "saved" can leave before the value's save dispatches,
		// dropping it. Wait for the settings PUT that actually carries this rule value
		// before returning, so the saved blocked rule set is guaranteed to include it.
		const valuePersisted = this.page.waitForResponse(
			(response) => {
				const request = response.request();
				if (
					request.method() !== "PUT" ||
					!/\/teams\/\d+$/.test(response.url())
				) {
					return false;
				}
				try {
					const body = JSON.parse(request.postData() ?? "");
					const ruleSet = body.blockedRuleSetJson
						? JSON.parse(body.blockedRuleSetJson)
						: null;
					return Boolean(
						ruleSet?.conditions?.some(
							(condition: { value?: string }) => condition.value === value,
						),
					);
				} catch {
					return false;
				}
			},
			{ timeout: 15_000 },
		);

		await valueInput.fill(value);
		await expect(valueInput).toHaveValue(value);
		await valuePersisted;
	}

	/**
	 * Opens a MUI Select and picks an option, waiting for the option menu to
	 * detach so its (invisible) backdrop cannot intercept the next click.
	 */
	private async chooseFromSelect(
		trigger: Locator,
		optionName: string,
	): Promise<void> {
		await trigger.click();
		await this.page
			.getByRole("option", { name: optionName, exact: true })
			.click();
		await this.page
			.getByRole("listbox")
			.waitFor({ state: "detached" })
			.catch(() => {});
	}
}

export class MetricsPage {
	page: Page;

	categoryWidgets: Record<MetricsCategories, [string, string][]> = {
		[MetricsCategories.FlowOverview]: [
			["Work In Progress Overview", "wipOverview"],
			["Flow Efficiency Overview", "flowEfficiency"],
			["Blocked Items Overview", "blockedOverview"],
			["Stale Items Overview", "staleOverview"],
			["Features Being Worked On Overview", "featuresWorkedOnOverview"],
			["Total Work Item Age Overview", "totalWorkItemAge"],
			["Predictability Score Overview", "predictabilityScore"],
			["Cycle Time Percentiles", "percentiles"],
			["Work Item Age Percentiles", "workItemAgePercentiles"],
			["Total Throughput", "totalThroughput"],
			["Total Arrivals", "totalArrivals"],
			["Feature Size Percentiles", "featureSizePercentiles"],
		],
		[MetricsCategories.FlowMetrics]: [
			["Cycle Time Scatterplot", "cycleScatter"],
			["Work Item Aging Chart", "aging"],
			["Cumulative Time per State", "stateTimeCumulative"],
			["Throughput Run Chart", "throughput"],
			["Work Items In Progress Over Time", "wipOverTime"],
			["Total Work Item Age Over Time", "totalWorkItemAgeOverTime"],
			["Arrivals Run Chart", "arrivals"],
			["Simplified Cumulative Flow Diagram", "stacked"],
			["Load Balance Matrix", "loadBalanceMatrix"],
			["Blocked Items Over Time", "blockedCountHistory"],
		],
		[MetricsCategories.Predictability]: [
			["Predictability Score Details", "predictabilityScoreDetails"],
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
