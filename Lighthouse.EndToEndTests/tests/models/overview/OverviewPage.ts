import { Page } from '@playwright/test';
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
        const projectLink = this.page.getByTestId(`project-card-${project.id}`).getByRole('link', { name: project.name });
        await projectLink.click();
        return new ProjectDetailPage(this.page);
    }

    async goToTeam(team: { name: string, id: number }): Promise<TeamDetailPage> {
        const teamLink = this.page.getByRole('link', { name: team.name });
        await teamLink.click();

        return new TeamDetailPage(this.page);
    }

    async getTeamsForProject(project: { name: string, id: number }): Promise<string[]> {
        const projectCard = this.page.getByTestId(`project-card-${project.id}`);
        const teamLinks = await projectCard.getByRole('link').allTextContents();
        return teamLinks;
    }

    async isProjectAvailable(project : {name: string, id: number}): Promise<boolean> {
        try{
            const projectLink = this.page.getByTestId(`project-card-${project.id}`).getByRole('link', { name: project.name });
            return projectLink.isVisible();
        }
        catch{
            return false;   
        }
    }
}
