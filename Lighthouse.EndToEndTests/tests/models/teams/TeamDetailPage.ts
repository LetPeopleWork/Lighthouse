import { expect, type Locator, type Page } from "@playwright/test";
import { getLastUpdatedDateFromText } from "../../helpers/dates";
import { TeamEditPage } from "./TeamEditPage";
import { WorkItemsInProgressDialog } from "./WorkItemsInProgressDialog";

export class TeamDetailPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async updateTeamData(): Promise<void> {
		await this.updateTeamDataButton.click();
	}

	async editTeam(): Promise<TeamEditPage> {
		await this.editTeamButton.click();

		return new TeamEditPage(this.page);
	}

	async toggleFeatures(): Promise<void> {
		await this.page.getByLabel("toggle").first().click();
	}

	async toggleForecast(): Promise<void> {
		await this.page.getByLabel("toggle").nth(1).click();
	}

	async forecast(howMany: number): Promise<number> {
		await this.page
			.getByLabel("Number of Work Items to Forecast")
			.fill(`${howMany}`);
		await this.page.getByRole("button", { name: "Forecast" }).nth(1).click();

		const likelihood =
			(await this.page.getByRole("heading", { name: "%" }).textContent()) ??
			"0";
		const parsedLikelihood = Number.parseFloat(likelihood.replace("%", ""));

		return parsedLikelihood;
	}

	async forecastNewWorkItems(workItemsTypes: string[]) {
		for (const itemType of workItemsTypes) {
			await this.page
				.getByRole("combobox", { name: "New Work Item Type" })
				.click();

			await this.page.keyboard.insertText(itemType);

			await this.page.keyboard.press("Enter");
		}

		await this.page.getByRole("button", { name: "Forecast" }).nth(2).click();
	}

	async goToMetrics(): Promise<void> {
		await this.page.getByRole("tab", { name: "Metrics" }).click();
	}

	async goToForecasts(): Promise<void> {
		await this.page.getByRole("tab", { name: "Forecasts" }).click();
	}

	async getLastUpdatedDate(): Promise<Date> {
		const lastUpdatedText =
			(await this.page
				.getByRole("heading", { name: /^Last Updated/ })
				.textContent()) ?? "";
		return getLastUpdatedDateFromText(lastUpdatedText);
	}

	async openWorkItemsInProgressDialog(): Promise<WorkItemsInProgressDialog> {
		await this.workItemsInProgressWidget
			.getByRole("heading", { name: "Work Items in Progress:" })
			.click();
		return new WorkItemsInProgressDialog(this.page);
	}

	async openClosedItemsDialog(): Promise<WorkItemsInProgressDialog> {
		await this.cycleTimePercentileWidget.click();
		return new WorkItemsInProgressDialog(this.page);
	}

	async openSleWidget(): Promise<void> {
		await this.sleWidgetButton.click();
	}

	async closeSleWidget(): Promise<void> {
		await this.returnToCycleTimePercentilesButton.click();
	}

	async openPredictabilityScoreWidget(): Promise<void> {
		await this.page
			.getByRole("button")
			.filter({ hasText: "Predictability Score" })
			.click();

		await expect(
			this.page.getByRole("heading", { name: "Predictability Score" }),
		).toBeVisible();
	}

	getFeatureLink(featureName: string): Locator {
		return this.page.getByRole("link", { name: featureName });
	}

	get updateTeamDataButton(): Locator {
		return this.page.getByRole("button", { name: "Update Team Data" });
	}

	get editTeamButton(): Locator {
		return this.page.getByRole("button", { name: "Edit" });
	}

	get teamId(): number {
		const url = new URL(this.page.url());
		const teamId = url.pathname.split("/").pop() ?? "0";
		return Number.parseInt(teamId, 10);
	}

	get cycleTimePercentileWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-percentiles")
			.locator("div")
			.filter({ hasText: /^Cycle Time PercentilesSLE:.*$/ })
			.first();
	}

	get cycleTimeScatterplotWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-cycleScatter")
			.locator("div")
			.filter({ hasText: /^Cycle Time50%70%85%95%Service.*$/ })
			.nth(1);
	}

	get throughputRunChartWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-throughput")
			.locator("div")
			.filter({ hasText: /^Work Items CompletedTotal:.*$/ })
			.nth(1);
	}

	get predictabilityScoreChartWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-throughput")
			.locator("div")
			.filter({ hasText: "Predictability Score" })
			.nth(1);
	}

	get wipOverTimeWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-wipOverTime")
			.locator("div")
			.filter({ hasText: /^Work Items In Progress Over.*$/ })
			.nth(1);
	}

	get workItemAgingChart(): Locator {
		return this.page
			.getByTestId("dashboard-item-aging")
			.locator("div")
			.filter({ hasText: /^Work Item Aging50%70%85%95%.*$/ })
			.nth(1);
	}

	get workDistributionChart(): Locator{
		return this.page
		.locator('div')
		.filter({ hasText: /^Work Distribution.*$/ })
		.nth(2)
	}

	get simplifiedCfdWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-stacked")
			.locator("div")
			.filter({ hasText: "Simplified Cumulative Flow" })
			.nth(1);
	}

	get sleWidgetButton(): Locator {
		return this.page.getByRole("button", { name: "SLE: 70% @ 7 days" });
	}

	get sleWidget(): Locator {
		return this.page.getByText(
			"Service Level ExpectationTarget:70% of all work items are done within 7 days or",
		);
	}

	get returnToCycleTimePercentilesButton(): Locator {
		return this.page
			.locator(".MuiCardContent-root > div > .MuiButtonBase-root")
			.first();
	}

	get workItemsInProgressWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-itemsInProgress")
			.locator("div")
			.filter({ hasText: /^Work Items in Progress:.*$/ })
			.first();
	}

	get startedVsClosedWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-startedVsFinished")
			.locator("div")
			.filter({ hasText: "Started vs. Closed Work" })
			.first();
	}

	get totalWorkItemAgeWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-totalWorkItemAge")
			.locator("div")
			.filter({ hasText: /^Total Work Item Age.*days$/ })
			.first();
	}

	get totalWorkItemAgeRunChart(): Locator {
		return this.page
			.getByTestId("dashboard-item-totalWorkItemAgeOverTime")
			.locator("div")
			.filter({ hasText: /^Work Items Total Work Item Age Over Time.*$/ })
			.nth(1);
	}
}
