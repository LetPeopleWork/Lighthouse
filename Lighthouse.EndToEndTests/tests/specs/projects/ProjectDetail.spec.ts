import {
	expect,
	test,
	testWithData,
	testWithUpdatedTeams,
} from "../../fixutres/LighthouseFixture";
import { expectDateToBeRecent } from "../../helpers/dates";

const testData = [
	{
		name: "Azure DevOps",
		index: 1,
		involvedTeams: [1],
		expectedFeatures: [
			{
				name: "Instant status monitoring for real-time insights",
				inProgress: false,
				defaultSize: false,
				involvedTeams: [1],
			},
			{
				name: "Intuitive content filtering",
				inProgress: false,
				defaultSize: true,
				involvedTeams: [1],
			},
		],
	},
	{
		name: "Jira",
		index: 2,
		involvedTeams: [2],
		expectedFeatures: [
			{
				name: "Majestic Moments",
				inProgress: false,
				defaultSize: true,
				involvedTeams: [2],
			},
			{
				name: "Astral Affinitiy",
				inProgress: true,
				defaultSize: false,
				involvedTeams: [2],
			},
		],
	},
];

for (const { index, name, involvedTeams, expectedFeatures } of testData) {
	testWithUpdatedTeams(involvedTeams)(
		`should show correct Features for ${name} project on refresh`,
		async ({ testData, overviewPage }) => {
			const project = testData.projects[index];

			const projectPage = await overviewPage.lightHousePage.goToProjects();
			const projectDetailPage = await projectPage.goToProject(project);

			const involvedTeams: { [key: string]: string[] } = {};

			await test.step("Refresh Features", async () => {
				await expect(projectDetailPage.refreshFeatureButton).toBeEnabled();

				await projectDetailPage.refreshFeatures();
				await expect(projectDetailPage.refreshFeatureButton).toBeDisabled();

				// Wait for update to be done
				await expect(projectDetailPage.refreshFeatureButton).toBeEnabled();

				const lastUpdatedDate = await projectDetailPage.getLastUpdatedDate();
				expectDateToBeRecent(lastUpdatedDate);
			});

			await test.step("Expected Features were loaded", async () => {
				for (const feature of expectedFeatures) {
					const featureLink = projectDetailPage.getFeatureLink(feature.name);
					await expect(featureLink).toBeVisible();

					if (feature.inProgress) {
						const inProgressIcon = projectDetailPage.getFeatureInProgressIcon(
							feature.name,
						);
						await expect(inProgressIcon).toBeVisible();
					}

					if (feature.defaultSize) {
						const defaultSizeIcon = projectDetailPage.getFeatureIsDefaultSize(feature.name);
						await expect(defaultSizeIcon).toBeVisible();
					}

					for (const involvedTeamIndex of feature.involvedTeams) {
						const team = testData.teams[involvedTeamIndex];

						if (!involvedTeams[team.name]) {
							involvedTeams[team.name] = [];
						}

						involvedTeams[team.name].push(feature.name);

						const teamLink = projectDetailPage.getTeamLinkForFeature(
							team.name,
							involvedTeams[team.name].length - 1,
						);
						await expect(teamLink).toBeVisible();
					}
				}
			});

			await test.step("Expect Team Detail to List Features", async () => {
				for (const [team, features] of Object.entries(involvedTeams)) {
					const teamsOverviewPage =
						await overviewPage.lightHousePage.goToTeams();

					const teamDetailPage = await teamsOverviewPage.goToTeam(team);

					for (const feature of features) {
						const featureLink = teamDetailPage.getFeatureLink(feature);
						await expect(featureLink).toBeVisible();
					}
				}
			});
		},
	);
}

testWithData(
	"should open Project Edit Page when clicking on Edit Button",
	async ({ testData, overviewPage }) => {
		const [project] = testData.projects;

		const projectsPage = await overviewPage.lightHousePage.goToProjects();
		const projectDetailPage = await projectsPage.goToProject(project);

		const projectEditPage = await projectDetailPage.editProject();
		expect(projectEditPage.page.url()).toContain(
			`/projects/edit/${project.id}`,
		);
	},
);

testWithUpdatedTeams([3])(
	"should include milestones and WIP in feature calculation",
	async ({ testData, overviewPage }) => {
		const project = testData.projects[2];

		const projectsPage = await overviewPage.lightHousePage.goToProjects();
		const projectDetailPage = await projectsPage.goToProject(project);

		await projectDetailPage.refreshFeatures();
		await expect(projectDetailPage.refreshFeatureButton).toBeEnabled();

		const milestoneName = "My Milestone";

		await test.step("Add Milestone", async () => {
			await projectDetailPage.toggleMilestoneConfiguration();

			const milestoneDate = new Date();
			milestoneDate.setDate(milestoneDate.getDate() + 14);

			await projectDetailPage.addMilestone(milestoneName, milestoneDate);
			const milestoneColumn = projectDetailPage.getMilestoneColumn(
				milestoneName,
				milestoneDate,
			);
			await expect(milestoneColumn).toBeVisible();

			const lastUpdatedTimeForFeature =
				await projectDetailPage.getLastUpdatedDateForFeature(
					"Majestic Moments",
				);
			expectDateToBeRecent(lastUpdatedTimeForFeature);
		});

		await test.step("Delete Milestone removes column", async () => {
			const milestoneDate = new Date();
			milestoneDate.setDate(new Date().getDate() + 7);
			await projectDetailPage.removeMilestone();
			const milestoneColumn = projectDetailPage.getMilestoneColumn(
				milestoneName,
				milestoneDate,
			);
			await expect(milestoneColumn).not.toBeVisible();
		});

		await test.step("Milestones in the past are hidden", async () => {
			const pastDate = new Date();
			pastDate.setDate(new Date().getDate() - 14);

			await projectDetailPage.addMilestone(milestoneName, pastDate);

			const milestoneColumn = projectDetailPage.getMilestoneColumn(
				milestoneName,
				pastDate,
			);
			await expect(milestoneColumn).not.toBeVisible();

			await projectDetailPage.removeMilestone();
		});

		await test.step("Change in Feature WIP recalculates Forecasts", async () => {
			const team = testData.teams[0];

			await projectDetailPage.toggleFeatureWIPConfiguration();

			await projectDetailPage.changeFeatureWIPForTeam(team.name, 2);

			const lastUpdatedTimeForFeature =
				await projectDetailPage.getLastUpdatedDateForFeature(
					"Majestic Moments",
				);
			expectDateToBeRecent(lastUpdatedTimeForFeature);
		});
	},
);
