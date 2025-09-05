import type { Locator, Page } from "@playwright/test";
import type { LighthousePage } from "../app/LighthousePage";
import { ProjectDeletionDialog } from "../projects/ProjectDeletionDialog";
import { ProjectDetailPage } from "../projects/ProjectDetailPage";
import { ProjectEditPage } from "../projects/ProjectEditPage";
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

	async getProjectLink(project: {
		name: string;
		id: number;
	}): Promise<Locator> {
		const projectLink = this.page.getByRole("link", { name: project.name });
		return projectLink;
	}

	async goToProject(project: {
		name: string;
		id: number;
	}): Promise<ProjectDetailPage> {
		await this.search(project.name);

		const projectLink = await this.getProjectLink(project);
		await projectLink.click();

		return new ProjectDetailPage(this.page);
	}

	async editProject(project: {
		name: string;
		id: number;
	}): Promise<ProjectEditPage> {
		await this.search(project.name);

		const projectEditIcon = this.page.getByLabel("Edit");
		await projectEditIcon.click();

		return new ProjectEditPage(this.page);
	}

	async addNewProject(): Promise<ProjectEditPage> {
		await this.page.getByRole("button", { name: "Add Project" }).click();

		return new ProjectEditPage(this.page);
	}

	async deleteProject(project: {
		name: string;
		id: number;
	}): Promise<ProjectDeletionDialog> {
		await this.search(project.name);
		const projectDeleteIcon = this.page.getByLabel("Delete");
		await projectDeleteIcon.click();

		return new ProjectDeletionDialog(this.page);
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
		await this.page.getByRole("button", { name: "Add Team" }).click();

		return new TeamEditPage(this.page);
	}

	async deleteTeam(teamName: string): Promise<TeamDeletionDialog> {
		await this.search(teamName);
		const teamDeleteIcon = this.page.getByLabel("Delete");
		await teamDeleteIcon.click();

		return new TeamDeletionDialog(this.page);
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
		return this.page.getByText("LighthouseOverviewSettings");
	}
}
