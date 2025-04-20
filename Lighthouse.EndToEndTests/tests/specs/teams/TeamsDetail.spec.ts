import { expect, testWithData } from "../../fixutres/LighthouseFixture";
import { expectDateToBeRecent } from "../../helpers/dates";

const testData = [
	{ name: "Azure DevOps", index: 0, expectedFeaturesInProgress: 1 },
	{ name: "Jira", index: 2, expectedFeaturesInProgress: 2 },
];

for (const { index, name, expectedFeaturesInProgress } of testData) {
	testWithData(
		`should update Team Data for ${name} team on click`,
		async ({ testData, overviewPage }) => {
			const team = testData.teams[index];

			const teamsPage = await overviewPage.lightHousePage.goToTeams();
			const teamDetailPage = await teamsPage.goToTeam(team.name);

			await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

			await teamDetailPage.updateTeamData();
			await expect(teamDetailPage.updateTeamDataButton).toBeDisabled();

			// Wait for update to be done
			await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

			const lastUpdatedDate = await teamDetailPage.getLastUpdatedDate();
			expectDateToBeRecent(lastUpdatedDate);

			// Check metrics
			await teamDetailPage.goToMetrics();

			// Add a small delay to give metrics time to load
			await teamDetailPage.page.waitForTimeout(300);

			await expect(teamDetailPage.workInProgressWidget).toBeVisible();
			await expect(teamDetailPage.cycleTimeScatterplotWidget).toBeVisible();
			await expect(teamDetailPage.throughputRunChartWidget).toBeVisible();
			await expect(teamDetailPage.wipOverTimeWidget).toBeVisible();
			await expect(teamDetailPage.cycleTimePercentileWidget).toBeVisible();
			await expect(teamDetailPage.getFeaturesInProgressWidget).toBeVisible();

			const actualFeaturesInProgress =
				await teamDetailPage.getFeaturesInProgress();
			expect(actualFeaturesInProgress).toBe(expectedFeaturesInProgress);
		},
	);
}

testWithData(
	"should open Team Edit Page when clicking on Edit Button",
	async ({ testData, overviewPage }) => {
		const [team] = testData.teams;

		const teamsPage = await overviewPage.lightHousePage.goToTeams();
		const teamDetailPage = await teamsPage.goToTeam(team.name);

		const teamEditPage = await teamDetailPage.editTeam();
		expect(teamEditPage.page.url()).toContain(`/teams/edit/${team.id}`);
	},
);

testWithData(
	"should show Manual When and How Many Forecast for team",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamsPage = await overviewPage.lightHousePage.goToTeams();
		const teamDetailPage = await teamsPage.goToTeam(team.name);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.updateTeamData();
		await expect(teamDetailPage.updateTeamDataButton).toBeDisabled();

		// Wait for update to be done
		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		const howMany = 20;
		const likelihood = await teamDetailPage.forecast(howMany);

		expect(likelihood).toBeGreaterThan(0);
	},
);
