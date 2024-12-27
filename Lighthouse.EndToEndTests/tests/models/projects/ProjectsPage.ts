import { Locator, Page } from '@playwright/test';
import { ProjectDetailPage } from './ProjectDetailPage';
import { ProjectEditPage } from './ProjectEditPage';
import { ProjectDeletionDialog } from './ProjectDeletionDialog';

export class ProjectsPage {
    page: Page;

    constructor(page: Page) {
        this.page = page;
    }

    async search(searchTerm: string): Promise<void> {
        await this.page.getByPlaceholder('Search').fill(searchTerm);
    }

    async getProjectLink(project: { name: string, id: number }): Promise<Locator> {
        const projectLink = this.page.getByRole('link', { name: project.name });
        return projectLink;
    }

    async goToProject(project: { name: string, id: number }): Promise<ProjectDetailPage> {
        await this.search(project.name);

        const projectLink = await this.getProjectLink(project);
        await projectLink.click();

        return new ProjectDetailPage(this.page);
    }

    async editProject(project: { name: string, id: number }): Promise<ProjectEditPage> {
        await this.search(project.name);

        const projectEditIcon = this.page.getByLabel('Edit');
        await projectEditIcon.click();

        return new ProjectEditPage(this.page);
    }

    async addNewProject() : Promise<ProjectEditPage> {
        await this.page.getByRole('button', { name: 'Add New' }).click();

        return new ProjectEditPage(this.page);
    }

    async deleteProject(project : { name: string, id: number }): Promise<ProjectDeletionDialog> {
        await this.search(project.name);
        const projectDeleteIcon = this.page.getByLabel('Delete');
        await projectDeleteIcon.click();

        return new ProjectDeletionDialog(this.page);
    }
}
