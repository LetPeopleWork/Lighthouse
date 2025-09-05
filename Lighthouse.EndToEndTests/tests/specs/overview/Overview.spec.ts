import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";

testWithData(
	"should show all projects on dashboard",
	async ({ testData, overviewPage }) => {
		const [project1, project2] = testData.projects;

		await expect(await overviewPage.getProjectLink(project1)).toBeVisible();
		await expect(await overviewPage.getProjectLink(project2)).toBeVisible();
	},
);

testWithData(
	"should filter projects on dashboard",
	async ({ testData, overviewPage }) => {
		const [project1, project2] = testData.projects;

		await test.step(`Search for Project ${project1.name}`, async () => {
			await overviewPage.search(project1.name);

			const projectLink = await overviewPage.getProjectLink(project1);

			await expect(projectLink).toBeVisible();
		});

		await test.step(`Search for Project ${project2.name}`, async () => {
			await overviewPage.search(project2.name);

			const projectLink = await overviewPage.getProjectLink(project2);
			await expect(projectLink).toBeVisible();
		});

		await test.step("Search for not existing Project", async () => {
			await overviewPage.search("Jambalaya");

			const projectLink1 = await overviewPage.getProjectLink(project1);
			const projectLink2 = await overviewPage.getProjectLink(project2);

			await expect(projectLink1).not.toBeVisible();
			await expect(projectLink2).not.toBeVisible();
		});

		await test.step("Clear Search", async () => {
			await overviewPage.search("");

			const projectLink1 = await overviewPage.getProjectLink(project1);
			const projectLink2 = await overviewPage.getProjectLink(project2);

			await expect(projectLink1).toBeVisible();
			await expect(projectLink2).toBeVisible();
		});
	},
);
