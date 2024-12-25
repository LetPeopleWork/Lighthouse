import { Page } from '@playwright/test';
import { LighthousePage } from '../app/LighthousePage';

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
