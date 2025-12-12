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
		`should show correct Features for ${name} portfolio on refresh`,
		async ({ testData, overviewPage }) => {
			const portfolio = testData.portfolios[index];

			const portfolioDetailPage = await overviewPage.goToPortfolio(portfolio);

			const involvedTeams: { [key: string]: string[] } = {};

			await test.step("Refresh Features", async () => {
				await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled();

				await portfolioDetailPage.refreshFeatures();
				await expect(portfolioDetailPage.refreshFeatureButton).toBeDisabled();

				// Wait for update to be done
				await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled();

				const lastUpdatedDate = await portfolioDetailPage.getLastUpdatedDate();
				expectDateToBeRecent(lastUpdatedDate);
			});

			await test.step("Expected Features were loaded", async () => {
				for (const feature of expectedFeatures) {
					const featureLink = portfolioDetailPage.getFeatureLink(feature.name);
					await expect(featureLink).toBeVisible();

					if (feature.inProgress) {
						const inProgressIcon = portfolioDetailPage.getFeatureInProgressIcon(
							feature.name,
						);
						await expect(inProgressIcon).toBeVisible();
					}

					if (feature.defaultSize) {
						const defaultSizeIcon = portfolioDetailPage.getFeatureIsDefaultSize(
							feature.name,
						);
						await expect(defaultSizeIcon).toBeVisible();
					}

					for (const involvedTeamIndex of feature.involvedTeams) {
						const team = testData.teams[involvedTeamIndex];

						if (!involvedTeams[team.name]) {
							involvedTeams[team.name] = [];
						}

						involvedTeams[team.name].push(feature.name);

						const teamLink = portfolioDetailPage.getTeamLinkForFeature(
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
						await overviewPage.lightHousePage.goToOverview();

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
	"should open Portfolio Edit Page when clicking on Edit Button",
	async ({ testData, overviewPage }) => {
		const [portfolio] = testData.portfolios;

		const portfolioDetailPage = await overviewPage.goToPortfolio(portfolio);

		const portfolioEditPage = await portfolioDetailPage.editPortfolio();
		expect(portfolioEditPage.page.url()).toContain(
			`/portfolios/edit/${portfolio.id}`,
		);
	},
);
