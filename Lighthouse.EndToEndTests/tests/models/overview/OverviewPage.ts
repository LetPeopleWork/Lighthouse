import type { Locator, Page } from "@playwright/test";
import type { LighthousePage } from "../app/LighthousePage";
import { PortfolioDeletionDialog } from "../portfolios/PortfolioDeletionDialog";
import { PortfolioDetailPage } from "../portfolios/PortfolioDetailPage";
import { PortfolioEditPage } from "../portfolios/PortfolioEditPage";
import { WorkTrackingSystemEditPage } from "../settings/WorkTrackingSystems/WorkTrackingSystemEditPage";
import { TeamDeletionDialog } from "../teams/TeamDeletionDialog";
import { TeamDetailPage } from "../teams/TeamDetailPage";
import { TeamEditPage } from "../teams/TeamEditPage";

export class OverviewPage {
	readonly page: Page;
	readonly lightHousePage: LighthousePage;

	constructor(page: Page, lightHousePage: LighthousePage) {
		this.page = page;
		this.lightHousePage = lightHousePage;
	}

	get lighthousePage(): LighthousePage {
		return this.lightHousePage;
	}

	async search(searchTerm: string): Promise<void> {
		await this.page.getByPlaceholder("Search").fill(searchTerm);
	}

	async getPortfolioLink(portfolio: {
		name: string;
		id: number;
	}): Promise<Locator> {
		const portfolioLink = this.page.getByRole("link", { name: portfolio.name });
		return portfolioLink;
	}

	async goToPortfolio(portfolio: {
		name: string;
		id: number;
	}): Promise<PortfolioDetailPage> {
		await this.search(portfolio.name);

		const portfolioLink = await this.getPortfolioLink(portfolio);
		await portfolioLink.click();

		return new PortfolioDetailPage(this.page);
	}

	async editPortfolio(portfolio: {
		name: string;
		id: number;
	}): Promise<PortfolioEditPage> {
		await this.search(portfolio.name);

		const portfolioEditIcon = this.page.getByLabel("Edit");
		await portfolioEditIcon.click();

		return new PortfolioEditPage(this.page);
	}

	async addNewPortfolio(): Promise<PortfolioEditPage> {
		await this.page
			.getByRole("button", { name: "Add Portfolio" })
			.first()
			.click();

		return new PortfolioEditPage(this.page);
	}

	async deletePortfolio(portfolio: {
		name: string;
		id: number;
	}): Promise<PortfolioDeletionDialog> {
		await this.search(portfolio.name);
		const portfolioDeleteIcon = this.page.getByLabel("Delete");
		await portfolioDeleteIcon.click();

		return new PortfolioDeletionDialog(this.page);
	}

	async addConnection(): Promise<WorkTrackingSystemEditPage> {
		await this.page
			.getByRole("button", { name: "Add Work Tracking System" })
			.first()
			.click();

		return new WorkTrackingSystemEditPage(this.page);
	}

	async deleteConnection(connectionName: string): Promise<void> {
		await this.search(connectionName);
		const connectionDeleteIcon = this.page.getByLabel("Delete");
		await connectionDeleteIcon.click();
	}

	async editConnection(
		connectionName: string,
	): Promise<WorkTrackingSystemEditPage> {
		await this.search(connectionName);

		const connectionEditIcon = this.page.getByLabel("Edit");
		await connectionEditIcon.click();

		return new WorkTrackingSystemEditPage(this.page);
	}

	getConnectionLink(connectionName: string): Locator {
		const connectionLink = this.page.getByRole("link", {
			name: connectionName,
		});
		return connectionLink;
	}

	async getTeamLink(teamName: string): Promise<Locator> {
		const teamLink = this.page.getByRole("link", { name: teamName });
		return teamLink;
	}

	async goToTeam(teamName: string): Promise<TeamDetailPage> {
		await this.search(teamName);

		const teamLink = await this.getTeamLink(teamName);
		await teamLink.click();

		return new TeamDetailPage(this.page);
	}

	async editTeam(teamName: string): Promise<TeamEditPage> {
		await this.search(teamName);

		const teamEditIcon = this.page.getByLabel("Edit");
		await teamEditIcon.click();

		return new TeamEditPage(this.page);
	}

	async addNewTeam(): Promise<TeamEditPage> {
		await this.page.getByRole("button", { name: "Add Team" }).first().click();

		return new TeamEditPage(this.page);
	}

	async deleteTeam(teamName: string): Promise<TeamDeletionDialog> {
		await this.search(teamName);
		const teamDeleteIcon = this.page.getByLabel("Delete");
		await teamDeleteIcon.click();

		return new TeamDeletionDialog(this.page);
	}

	async cloneTeam(teamName: string): Promise<TeamEditPage> {
		await this.search(teamName);
		const teamCloneIcon = this.page.getByLabel("Clone");
		await teamCloneIcon.click();

		return new TeamEditPage(this.page);
	}

	async clonePortfolio(portfolioName: string): Promise<PortfolioEditPage> {
		await this.search(portfolioName);

		const portfolioCloneIcon = this.page.getByLabel("Clone");

		await portfolioCloneIcon.click();

		return new PortfolioEditPage(this.page);
	}

	async showLicenseTooltip(): Promise<void> {
		await this.page.getByTestId("license-status-button").hover();
		await this.page
			.getByText("License valid - Click for")
			.waitFor({ state: "visible" });
	}

	async showLicensingInformation(): Promise<Locator> {
		await this.page.getByTestId("license-status-button").click();

		return this.page
			.locator("div")
			.filter({ hasText: /Licensed to:/ })
			.nth(1);
	}

	get toolbar(): Locator {
		return this.page.getByText("LighthouseOverviewSystem Settings");
	}
}
