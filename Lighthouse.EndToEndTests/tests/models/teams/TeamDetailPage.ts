import type { Locator, Page } from "@playwright/test";
import { getLastUpdatedDateFromText } from "../../helpers/dates";
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
		await this.editTeamButton.click();

		return new TeamEditPage(this.page);
	}

	async toggleFeatures(): Promise<void> {
		await this.page.getByLabel("toggle").first().click();
	}

	async toggleThroughput(): Promise<void> {
		await this.page.getByLabel("toggle").nth(2).click();
	}

	async toggleForecast(): Promise<void> {
		await this.page.getByLabel("toggle").nth(1).click();
	}

	async forecast(howMany: number): Promise<number> {
		await this.page
			.getByLabel("Number of Items to Forecast")
			.fill(`${howMany}`);
		await this.page.getByRole("button", { name: "Forecast" }).click();

		const likelihood =
			(await this.page.getByRole("heading", { name: "%" }).textContent()) ??
			"0";
		const parsedLikelihood = Number.parseFloat(likelihood.replace("%", ""));

		return parsedLikelihood;
	}

	async getFeaturesInProgress(): Promise<number> {
		const featureWIPText =
			(await this.page
				.getByRole("heading", { name: /^Currently working on/ })
				.textContent()) ?? "0";
		const match = /Currently working on (\d+)/.exec(featureWIPText);
		return Number(match?.[1] ?? 0);
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

	get editTeamButton(): Locator {
		return this.page.getByRole("button", { name: "Edit" });
	}

	get teamId(): number {
		const url = new URL(this.page.url());
		const teamId = url.pathname.split("/").pop() ?? "0";
		return Number.parseInt(teamId, 10);
	}
}
