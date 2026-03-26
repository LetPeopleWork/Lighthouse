import type { Locator, Page } from "@playwright/test";
import { LoginPage } from "../auth/LoginPage";
import { SessionExpiredPage } from "../auth/SessionExpiredPage";
import { OverviewPage } from "../overview/OverviewPage";
import { PortfolioEditPage } from "../portfolios/PortfolioEditPage";
import { SettingsPage } from "../settings/SettingsPage";
import { TeamEditPage } from "../teams/TeamEditPage";

export class LighthousePage {
	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async openWithAuth(): Promise<LoginPage> {
		await this.page.goto("/");
		return new LoginPage(this.page);
	}

	async open(): Promise<OverviewPage> {
		await this.page.goto("/");
		return this.goToOverview();
	}

	async goToOverview(): Promise<OverviewPage> {
		await this.page.getByRole("link", { name: "Overview" }).click();
		return new OverviewPage(this.page, this);
	}

	async createNewTeam(): Promise<TeamEditPage> {
		await this.page.goto("/teams/new");
		return new TeamEditPage(this.page);
	}

	async createNewProject(): Promise<PortfolioEditPage> {
		await this.page.goto("/portfolios/new");
		return new PortfolioEditPage(this.page);
	}

	async goToSettings(): Promise<SettingsPage> {
		await this.page.getByRole("link", { name: "Settings" }).click();
		return new SettingsPage(this.page);
	}

	async goToContributors(): Promise<Page> {
		return this.OpenInNewTab(async () => {
			const contributorsButton = this.GetContributorsButton();
			await contributorsButton.click();
		});
	}

	async goToDocumentation(): Promise<Page> {
		return this.OpenInNewTab(async () => {
			const documentationButton = this.GetDocumentationButton();
			await documentationButton.click();
		});
	}

	async goToLetPeopleWork(): Promise<Page> {
		return this.OpenInNewTab(async () => {
			const lpwLogo = this.GetLpwLogoButton();
			await lpwLogo.click();
		});
	}

	async goToRelease(): Promise<Page> {
		return this.OpenInNewTab(async () => {
			const versionNumberButton = this.GetVersionNumberButton();
			await versionNumberButton.click();
		});
	}

	async getEmailContact(): Promise<string> {
		const emailButton = this.page.getByTestId("mailto:contact@letpeople.work");
		const link = await emailButton.getAttribute("href");
		return link ?? "";
	}

	async contactViaCall(): Promise<Page> {
		return this.OpenInNewTab(async () => {
			const callButton = this.page.getByTestId(
				"https://calendly.com/let-people-work",
			);
			await callButton.click();
		});
	}

	async contactViaLinkedIn(): Promise<Page> {
		return this.OpenInNewTab(async () => {
			const linkedInButton = this.page.getByTestId(
				"https://www.linkedin.com/company/let-people-work/?viewAsMember=true",
			);
			await linkedInButton.click();
		});
	}

	async contactViaGitHubIssue(): Promise<Page> {
		return this.OpenInNewTab(async () => {
			const raiseGitHubIssueButton = this.page.getByLabel(
				"Raise an Issue on GitHub",
			);
			await raiseGitHubIssueButton.click();
		});
	}

	async clearCookies(): Promise<SessionExpiredPage> {
		await this.page.context().clearCookies();
		return new SessionExpiredPage(this.page);
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

	async clearLicense(): Promise<void> {
		await this.showLicensingInformation();

		await this.page.getByRole("button", { name: "Clear License" }).click();

		// Confirmation Dialog
		await this.page.getByRole("button", { name: "Clear License" }).click();
	}

	async logout(): Promise<LoginPage> {
		await this.page.getByTestId("logout-button").click();
		return new LoginPage(this.page);
	}

	private GetContributorsButton(): Locator {
		return this.page.getByTestId(
			"https://github.com/LetPeopleWork/Lighthouse/blob/main/CONTRIBUTORS.md",
		);
	}

	private GetDocumentationButton(): Locator {
		return this.page.getByTestId("https://docs.lighthouse.letpeople.work");
	}

	private GetLpwLogoButton(): Locator {
		return this.page.getByRole("link", { name: "Let People Work Logo" });
	}

	private GetVersionNumberButton(): Locator {
		// Match version number scheme like 'v1.33.7' or 'v24.12.20.1852'
		return this.page.getByRole("link", { name: /^v\d{2,4}(\.\d{1,4}){2,3}$/ });
	}

	private async OpenInNewTab(
		openInNewTabAction: () => Promise<void>,
	): Promise<Page> {
		const popup = this.page.waitForEvent("popup");
		await openInNewTabAction();
		return await popup;
	}
}
