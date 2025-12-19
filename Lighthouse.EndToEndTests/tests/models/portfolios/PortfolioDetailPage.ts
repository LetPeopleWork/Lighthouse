import type { Locator, Page } from "@playwright/test";
import { getLastUpdatedDateFromText } from "../../helpers/dates";
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
		const inProgressIcon = this.page
			.getByRole("gridcell", { name: feature })
			.getByRole("button")
			.first();
		return inProgressIcon;
	}

	getFeatureIsDefaultSize(featureName: string): Locator {
		const defaultSizeIcon = this.page
			.getByRole("gridcell")
			.filter({ hasText: featureName })
			.getByLabel("No child Work Items were found for");
		return defaultSizeIcon;
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

	async toggleFeatureWIPConfiguration(): Promise<void> {
		await this.page.getByLabel("toggle").nth(0).click();
	}

	async changeFeatureWIPForTeam(teamName: string, featureWIP: number) {
		await this.page.getByLabel(teamName).fill(`${featureWIP}`);
		await this.page.getByLabel(teamName).press("Enter");
	}

	async editPortfolio(): Promise<PortfolioEditPage> {
		await this.editPorftolioButton.click();

		return new PortfolioEditPage(this.page);
	}

	async refreshFeatures(): Promise<void> {
		await this.refreshFeatureButton.click();
	}

	async goToMetrics(): Promise<void> {
		await this.page.getByRole("tab", { name: "Metrics" }).click();
	}

	async goToDeliveries(): Promise<DeliveriesPage> {
		await this.page.getByRole("tab", { name: "Deliveries" }).click();

		return new DeliveriesPage(this.page);
	}

	get refreshFeatureButton(): Locator {
		return this.page.getByRole("button", { name: "Refresh Features" });
	}

	get editPorftolioButton(): Locator {
		return this.page.getByRole("button", { name: "Edit Portfolio" });
	}

	get featureSizeWidget(): Locator {
		return this.page
			.getByTestId("dashboard-item-featureSize")
			.locator("div")
			.filter({ hasText: "Features Size50%70%85%95%To" })
			.nth(1);
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
			.filter({ hasText: /^Features Total Work Item Age Over Time.*$/ })
			.nth(1);
	}

	get portfolioId(): number {
		const url = new URL(this.page.url());
		const portfolioId = url.pathname.split("/").pop() ?? "0";
		return Number.parseInt(portfolioId, 10);
	}
}
