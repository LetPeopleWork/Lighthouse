import { Page } from '@playwright/test';

export class OverviewPage {
    readonly page: Page;

    constructor(page: Page) {
        this.page = page;
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
