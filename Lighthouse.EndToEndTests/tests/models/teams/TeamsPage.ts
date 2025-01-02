import type { Locator, Page } from "@playwright/test";
import { TeamDeletionDialog } from "./TeamDeletionDialog";
import { TeamDetailPage } from "./TeamDetailPage";
import { TeamEditPage } from "./TeamEditPage";

export class TeamsPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async search(searchTerm: string): Promise<void> {
		await this.page.getByPlaceholder("Search").fill(searchTerm);
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
		await this.page.getByRole("button", { name: "Add New" }).click();

		return new TeamEditPage(this.page);
	}

	async deleteTeam(teamName: string): Promise<TeamDeletionDialog> {
		await this.search(teamName);
		const teamDeleteIcon = this.page.getByLabel("Delete");
		await teamDeleteIcon.click();

		return new TeamDeletionDialog(this.page);
	}
}
