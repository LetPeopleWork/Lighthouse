import {
	expect,
	test,
	testWithDemoData,
} from "../../fixutres/LighthouseFixture";

const PRODUCT_LAUNCH_SCENARIO_ID = 2;
const testWithPortfolio = testWithDemoData(PRODUCT_LAUNCH_SCENARIO_ID);

testWithPortfolio(
	"should handle Owning Team correctly",
	async ({ testData, overviewPage }) => {
		const portfolio = testData.portfolios[0];

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
			for (const team of testData.teams) {
				expect(availableOptions).toContain(team.name);
			}
		});
	},
);
