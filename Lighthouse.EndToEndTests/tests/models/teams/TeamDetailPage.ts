import { type Locator, type Page, expect } from "@playwright/test";
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
		await this.page
			.getByRole("button", { name: "Forecast", exact: true })
			.click();

		const likelihood =
			(await this.page.getByRole("heading", { name: "%" }).textContent()) ??
			"0";
		const parsedLikelihood = Number.parseFloat(likelihood.replace("%", ""));

		return parsedLikelihood;
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

	async getFeaturesInProgress(): Promise<number> {
		const featuresInProgressText = await this.page
			.getByText(/Features being Worked On:(\d+)/)
			.first()
			.innerText();

		// Extract just the number after "Features being Worked On:"
		const regex = /Features being Worked On:\s*(\d+)/;
		const match = regex.exec(featuresInProgressText);
		const count = match ? Number.parseInt(match[1], 10) : 0;

		return count;
	}

	async openWorkItemsInProgressDialog(): Promise<WorkItemsInProgressDialog> {
		await this.workItemsInProgressWidget.click();
		return new WorkItemsInProgressDialog(this.page);
	}

	async openFeaturesInProgressDialog(): Promise<WorkItemsInProgressDialog> {
		await this.featuresInProgressWidget.click();
		return new WorkItemsInProgressDialog(this.page);
	}

	async openBlockedItemsDialog(): Promise<WorkItemsInProgressDialog> {
		await this.blockedItemsWidget.click();
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
			.locator("div")
			.filter({ hasText: /^Cycle Time Percentiles.*$/ })
			.nth(1);
	}

	get cycleTimeScatterplotWidget(): Locator {
		return this.page.locator("div:nth-child(6) > .MuiPaper-root");
	}

	get throughputRunChartWidget(): Locator {
		return this.page.locator("div:nth-child(5) > .MuiPaper-root");
	}

	get wipOverTimeWidget(): Locator {
		return this.page.locator("div:nth-child(8) > .MuiPaper-root");
	}

	get workItemAgingChart(): Locator {
		return this.page.locator("div:nth-child(7) > .MuiPaper-root");
	}

	get simplifiedCfdWidget(): Locator {
		return this.page.locator("div:nth-child(9) > .MuiPaper-root");
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
			.locator("div")
			.filter({ hasText: /^Work Items in Progress:.*$/ })
			.first();
	}

	get featuresInProgressWidget(): Locator {
		return this.page
			.locator("div")
			.filter({ hasText: /^Features being Worked On:.*$/ })
			.first();
	}

	get blockedItemsWidget(): Locator {
		return this.page
			.locator("div")
			.filter({ hasText: /^Blocked:.*$/ })
			.first();
	}

	get startedVsClosedWidget(): Locator {
		return this.page.locator("div:nth-child(4) > .MuiPaper-root");
	}
}
