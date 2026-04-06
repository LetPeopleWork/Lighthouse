import type { Locator, Page } from "@playwright/test";
import { getLastUpdatedDateFromText } from "../../helpers/dates";
import { MetricsPage } from "../metrics/MetricsPage";
import { TeamEditPage } from "./TeamEditPage";

export class TeamDetailPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async updateTeamData(): Promise<void> {
		await this.updateTeamDataButton.click();
	}

	async editTeam(): Promise<TeamEditPage> {
		await this.goToSettings();

		return new TeamEditPage(this.page);
	}

	async goToSettings(): Promise<void> {
		await this.page.getByRole("tab", { name: "Settings" }).click();
	}

	async getNumberOfFeatures(): Promise<number> {
		const grid = this.page.getByRole("grid");

		// MUI puts data rows inside a rowgroup
		const rows = grid.getByRole("row").filter({
			hasNot: grid.getByRole("columnheader"),
		});

		return await rows.count();
	}

	async forecast(howMany: number, targetDate?: Date): Promise<number> {
		await this.page
			.getByRole("spinbutton", { name: "Number of Work Items" })
			.fill(`${howMany}`);

		if (targetDate) {
			const month = (targetDate.getMonth() + 1).toString();
			const day = targetDate.getDate().toString();
			const year = targetDate.getFullYear().toString();

			const targetDateGroup = this.page
				.getByRole("group", { name: "Target Date" })
				.first();

			await targetDateGroup
				.getByRole("spinbutton", { name: "Month" })
				.fill(month);

			await targetDateGroup.getByRole("spinbutton", { name: "Day" }).fill(day);

			await targetDateGroup
				.getByRole("spinbutton", { name: "Year" })
				.fill(year);
		}

		await this.page.getByRole("button", { name: "Forecast" }).first().click();

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

		await this.page.getByRole("button", { name: "Forecast" }).nth(1).click();
	}

	async runBacktest(): Promise<void> {
		await this.page.getByRole("button", { name: "Run Backtest" }).click();
	}

	get backtestResultsSection(): Locator {
		return this.page.getByText("Backtest Results");
	}

	get backtestForecastingSection(): Locator {
		return this.page.getByRole("heading", { name: "Forecast Backtesting" });
	}

	async clickBacktestHistoricalThroughputTab(): Promise<void> {
		await this.page.getByRole("tab", { name: "Historical Throughput" }).click();
	}

	get backtestHistoricalThroughputChart(): Locator {
		return this.page.getByText("Historical Throughput").locator("../..");
	}

	async goToFeatures(): Promise<void> {
		await this.page.getByRole("tab", { name: "Features" }).click();
	}

	async goToMetrics(): Promise<MetricsPage> {
		await this.page.getByRole("tab", { name: "Metrics" }).click();
		return new MetricsPage(this.page);
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

	getFeatureLink(featureName: string): Locator {
		return this.page.getByRole("link", { name: featureName });
	}

	get updateTeamDataButton(): Locator {
		return this.page.getByRole("button", { name: "Update Team Data" });
	}

	get teamId(): number {
		const url = new URL(this.page.url());
		const teamId = url.pathname.split("/").pop() ?? "0";
		return Number.parseInt(teamId, 10);
	}
}
