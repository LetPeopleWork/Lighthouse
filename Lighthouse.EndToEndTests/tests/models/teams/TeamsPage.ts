import { Locator, Page } from '@playwright/test';
import { TeamDetailPage } from './TeamDetailPage';
import { TeamEditPage } from './TeamEditPage';
import { TeamDeletionDialog } from './TeamDeletionDialog';

export class TeamsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async search(searchTerm: string): Promise<void> {
        await this.page.getByPlaceholder('Search').fill(searchTerm);
    }

    async getTeamLink(team: { name: string, id: number }): Promise<Locator> {
        const teamLink = this.page.getByRole('link', { name: team.name });
        return teamLink;
    }

    async goToTeam(team: { name: string, id: number }): Promise<TeamDetailPage> {
        await this.search(team.name);

        const teamLink = await this.getTeamLink(team);
        await teamLink.click();

        return new TeamDetailPage(this.page);
    }

    async editTeam(team: { name: string, id: number }): Promise<TeamEditPage> {
        await this.search(team.name);

        const teamEditIcon = this.page.getByLabel('Edit');
        await teamEditIcon.click();

        return new TeamEditPage(this.page);
    }
    
    async addNewTeam() : Promise<TeamEditPage> {
        await this.page.getByRole('button', { name: 'Add New' }).click();

        return new TeamEditPage(this.page);
    }

    async deleteTeam(team : { name: string, id: number }): Promise<TeamDeletionDialog> {
        await this.search(team.name);
        const teamDeleteIcon = this.page.getByLabel('Delete');
        await teamDeleteIcon.click();

        return new TeamDeletionDialog(this.page);
    }
}
