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

	/**
	 * Addressed by the name cell rather than by the row's text: a row now carries the names of the
	 * Features it waits on as well as its own, so matching anywhere in the row finds both ends of a
	 * dependency and Playwright refuses to choose between them.
	 */
	getFeatureRow(featureName: string): Locator {
		return this.page.getByRole("row").filter({
			has: this.page.locator('[data-field="name"]', { hasText: featureName }),
		});
	}

	getFeatureInProgressIcon(feature: string): Locator {
		return this.getFeatureRow(feature).getByTestId("active-work-indicator");
	}

	getFeatureForecastCell(featureName: string): Locator {
		return this.getFeatureRow(featureName).getByTestId("feature-forecast-cell");
	}

	getFeatureHasWarning(featureName: string): Locator {
		return this.getFeatureRow(featureName).getByTestId("warnings");
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
		return new MetricsPage(this.page, "portfolio");
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
