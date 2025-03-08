import type { Locator, Page } from "@playwright/test";
import { getLastUpdatedDateFromText } from "../../helpers/dates";
import { ProjectEditPage } from "./ProjectEditPage";

export class ProjectDetailPage {
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
			.getByRole("cell", { name: feature })
			.getByRole("button")
			.first();
		return inProgressIcon;
	}

	getFeatureIsDefaultSize(): Locator {
		const defaultSizeIcon = this.page.getByLabel(
			"No child items were found for",
		);
		return defaultSizeIcon;
	}

	getTeamLinkForFeature(teamName: string, index: number): Locator {
		const teamLink = this.page.getByRole("link", { name: teamName }).nth(index);
		return teamLink;
	}

	async getLastUpdatedDateForFeature(featureName: string): Promise<Date> {
		const featureRow = this.page.locator(`tr:has-text("${featureName}")`);
		const featureRowText = await featureRow.textContent();

		const datePattern =
			/(\d{1,2}\/\d{1,2}\/\d{4},\s\d{1,2}:\d{2}:\d{2}\s[AP]M)$/;
		const match = featureRowText?.match(datePattern);
		if (!match) {
			return new Date();
		}

		return new Date(match[1]);
	}

	async getLastUpdatedDate(): Promise<Date> {
		const lastUpdatedText =
			(await this.page
				.getByRole("heading", { name: /^Last Updated/ })
				.textContent()) ?? "";
		return getLastUpdatedDateFromText(lastUpdatedText);
	}

	async toggleMilestoneConfiguration(): Promise<void> {
		await this.page.getByLabel("toggle").first().click();
	}

	async addMilestone(name: string, date: Date): Promise<void> {
		await this.page.getByLabel("New Milestone Name").fill(name);
		await this.page
			.getByLabel("New Milestone Date")
			.fill(date.toISOString().split("T")[0]);
		await this.page.getByRole("button", { name: "Add Milestone" }).click();
	}

	async removeMilestone(): Promise<void> {
		await this.page.getByLabel("delete").click();
	}

	getMilestoneColumn(milestoneName: string, milestoneDate: Date): Locator {
		const dateString = milestoneDate.toLocaleDateString("en-US");
		return this.page.getByRole("columnheader", {
			name: `${milestoneName} (${dateString})`,
		});
	}

	async toggleFeatureWIPConfiguration(): Promise<void> {
		await this.page.getByLabel("toggle").nth(1).click();
	}

	async changeFeatureWIPForTeam(teamName: string, featureWIP: number) {
		await this.page.getByLabel(teamName).fill(`${featureWIP}`);
		await this.page.getByLabel(teamName).press("Enter");
	}

	async editProject(): Promise<ProjectEditPage> {
		await this.editProjectButton.click();

		return new ProjectEditPage(this.page);
	}

	async refreshFeatures(): Promise<void> {
		await this.refreshFeatureButton.click();
	}

	get refreshFeatureButton(): Locator {
		return this.page.getByRole("button", { name: "Refresh Features" });
	}

	get editProjectButton(): Locator {
		return this.page.getByRole("button", { name: "Edit Project" });
	}

	get projectId(): number {
		const url = new URL(this.page.url());
		const projectId = url.pathname.split("/").pop() ?? "0";
		return Number.parseInt(projectId, 10);
	}
}
