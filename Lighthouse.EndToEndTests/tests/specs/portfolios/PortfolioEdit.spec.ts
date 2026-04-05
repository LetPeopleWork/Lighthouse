import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";

testWithData(
	"should handle Owning Team correctly",
	async ({ testData, overviewPage }) => {
		const portfolio = testData.portfolios[0];
		const [team1, team2, team3] = testData.teams;

		const portfolioEditPage = await overviewPage.editPortfolio(portfolio);

		await test.step("No Team is selected as Owner by Default", async () => {
			await portfolioEditPage.toggleOwnershipSettings();

			const owningTeamValue = await portfolioEditPage.getSelectedOwningTeam();
			expect(owningTeamValue).toBe("​");
		});

		await test.step("All Available Teams and None can be selected as Owner", async () => {
			const availableOptions =
				await portfolioEditPage.getPotentialOwningTeams();

			expect(availableOptions).toContain("None");
			expect(availableOptions).toContain(team1.name);
			expect(availableOptions).toContain(team2.name);
			expect(availableOptions).toContain(team3.name);
		});
	},
);
