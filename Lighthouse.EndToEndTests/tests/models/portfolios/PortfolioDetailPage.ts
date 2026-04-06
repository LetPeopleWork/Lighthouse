import type { Locator, Page } from "@playwright/test";
import { getLastUpdatedDateFromText } from "../../helpers/dates";
import { MetricsPage } from "../metrics/MetricsPage";
import { DeliveriesPage } from "./Deliveries/DeliveriesPage";
import { PortfolioEditPage } from "./PortfolioEditPage";

export class PortfolioDetailPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	getFeatureLink(feature: string): Locator {
		const featureLink = this.page.getByRole("link", { name: feature });
		return featureLink;
	}

	getFeatureInProgressIcon(feature: string): Locator {
		return this.page
			.getByRole("row")
			.filter({
				has: this.page.getByRole("gridcell").filter({ hasText: feature }),
			})
			.getByTestId("active-work-indicator");
	}

	getFeatureHasWarning(featureName: string): Locator {
		const warningIcon = this.page
			.getByRole("row")
			.filter({ hasText: featureName })
			.getByTestId("warning-default-feature-size");
		return warningIcon;
	}

	getTeamLinkForFeature(teamName: string, index: number): Locator {
		const teamLink = this.page.getByRole("link", { name: teamName }).nth(index);
		return teamLink;
	}

	async getLastUpdatedDate(): Promise<Date> {
		const lastUpdatedText =
			(await this.page
				.getByRole("heading", { name: /^Last Updated/ })
				.textContent()) ?? "";
		return getLastUpdatedDateFromText(lastUpdatedText);
	}

	async editPortfolio(): Promise<PortfolioEditPage> {
		await this.goToSettings();

		return new PortfolioEditPage(this.page);
	}

	async goToSettings(): Promise<void> {
		await this.page.getByRole("tab", { name: "Settings" }).click();
	}

	async refreshFeatures(): Promise<void> {
		await this.refreshFeatureButton.click();
	}

	async goToMetrics(): Promise<MetricsPage> {
		await this.page.getByRole("tab", { name: "Metrics" }).click();
		return new MetricsPage(this.page);
	}

	async goToDeliveries(): Promise<DeliveriesPage> {
		await this.page.getByRole("tab", { name: "Deliveries" }).click();

		return new DeliveriesPage(this.page);
	}

	get refreshFeatureButton(): Locator {
		return this.page.getByRole("button", { name: "Refresh Features" });
	}

	get portfolioId(): number {
		const url = new URL(this.page.url());
		const portfolioId = url.pathname.split("/").pop() ?? "0";
		return Number.parseInt(portfolioId, 10);
	}
}
