import { Locator, Page } from '@playwright/test';
import { LighthousePage } from '../app/LighthousePage';
import { ProjectDetailPage } from '../projects/ProjectDetailPage';
import { TeamDetailPage } from '../teams/TeamDetailPage';

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
        await this.page.getByPlaceholder('Search').fill(searchTerm);
    }

    async goToProject(project: { name: string, id: number }): Promise<ProjectDetailPage> {
        const projectLink = await this.getProjectLink(project);
        await projectLink.click();
        return new ProjectDetailPage(this.page);
    }

    async goToTeam(team: { name: string, id: number }): Promise<TeamDetailPage> {
        const teamLink = this.page.getByRole('link', { name: team.name });
        await teamLink.click();

        return new TeamDetailPage(this.page);
    }

    async getTeamsForProject(project: { name: string, id: number }): Promise<string[]> {
        const projectCard = await this.getProjectCard(project);
        const teamLinks = await projectCard.getByRole('link').allTextContents();
        return teamLinks;
    }

    async getProjectCard(project: { name: string, id: number }): Promise<Locator> {
        const projectCard = this.page.getByTestId(`project-card-${project.id}`);
        return projectCard;
    }

    async getProjectLink(project: { name: string, id: number }): Promise<Locator> {
        const projectCard = await this.getProjectCard(project);
        const projectLink = projectCard.getByRole('link', { name: project.name });
        return projectLink;
    }
}
