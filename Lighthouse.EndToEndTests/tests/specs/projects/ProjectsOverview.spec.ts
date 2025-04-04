import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";

testWithData(
	"should show all projects on Projects Overview",
	async ({ testData, overviewPage }) => {
		const [project1, project2, project3] = testData.projects;

		const projectsPage = await overviewPage.lightHousePage.goToProjects();

		const projectLink1 = await projectsPage.getProjectLink(project1);
		const projectLink2 = await projectsPage.getProjectLink(project2);
		const projectLink3 = await projectsPage.getProjectLink(project3);

		await expect(projectLink1).toBeVisible();
		await expect(projectLink2).toBeVisible();
		await expect(projectLink3).toBeVisible();
	},
);

testWithData(
	"should filter projects on Project Overview",
	async ({ testData, overviewPage }) => {
		const [project1, project2] = testData.projects;

		const projectsPage = await overviewPage.lightHousePage.goToProjects();

		await test.step(`Search for project ${project1.name}`, async () => {
			await projectsPage.search(project1.name);

			const projectLink1 = await projectsPage.getProjectLink(project1);
			const projectLink2 = await projectsPage.getProjectLink(project2);

			await expect(projectLink1).toBeVisible();
			await expect(projectLink2).not.toBeVisible();
		});

		await test.step(`Search for project ${project2.name}`, async () => {
			await projectsPage.search(project2.name);

			const projectLink1 = await projectsPage.getProjectLink(project1);
			const projectLink2 = await projectsPage.getProjectLink(project2);

			await expect(projectLink1).not.toBeVisible();
			await expect(projectLink2).toBeVisible();
		});

		await test.step("Search for not existing project", async () => {
			await projectsPage.search("Jambalaya");

			const projectLink1 = await projectsPage.getProjectLink(project1);
			const projectLink2 = await projectsPage.getProjectLink(project2);

			await expect(projectLink1).not.toBeVisible();
			await expect(projectLink2).not.toBeVisible();
		});
	},
);

testWithData(
	"should open project Info when clicking on project",
	async ({ testData, overviewPage }) => {
		const [project1] = testData.projects;

		const projectsPage = await overviewPage.lightHousePage.goToProjects();

		const projectDetailPage = await projectsPage.goToProject(project1);
		expect(projectDetailPage.page.url()).toContain(`/projects/${project1.id}`);
	},
);

testWithData(
	"should open project Edit Page when clicking on Edit Icon",
	async ({ testData, overviewPage }) => {
		const [project1] = testData.projects;

		const projectsPage = await overviewPage.lightHousePage.goToProjects();

		const projectDetailPage = await projectsPage.editProject(project1);
		expect(projectDetailPage.page.url()).toContain(
			`/projects/edit/${project1.id}`,
		);
	},
);

testWithData(
	"should delete project when clicking on Delete Icon and confirming",
	async ({ testData, overviewPage }) => {
		const [project1] = testData.projects;

		const projectsPage = await overviewPage.lightHousePage.goToProjects();

		await test.step(`Delete project ${project1.name}`, async () => {
			const projectDeletionDialog = await projectsPage.deleteProject(project1);
			await projectDeletionDialog.delete();
		});

		await test.step(`Search for project ${project1.name}`, async () => {
			await projectsPage.search(project1.name);
			const projectLink = await projectsPage.getProjectLink(project1);

			await expect(projectLink).not.toBeVisible();
		});
	},
);

testWithData(
	"should not delete project when clicking on Delete Icon and cancelling",
	async ({ testData, overviewPage }) => {
		const [project1] = testData.projects;

		const projectsPage = await overviewPage.lightHousePage.goToProjects();

		await test.step(`Delete project ${project1.name}`, async () => {
			const projectDeletionDialog = await projectsPage.deleteProject(project1);
			await projectDeletionDialog.cancel();
		});

		await test.step(`Search for project ${project1.name}`, async () => {
			await projectsPage.search(project1.name);

			const projectLink = await projectsPage.getProjectLink(project1);
			await expect(projectLink).toBeVisible();
		});
	},
);
