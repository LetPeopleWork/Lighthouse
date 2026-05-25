import { expect, testWithUpdatedTeams } from "../../fixutres/LighthouseFixture";
import { MetricsCategories } from "../../models/metrics/MetricsPage";

const adoBackedTeam = testWithUpdatedTeams([0]);

adoBackedTeam(
	"flow coach sees how long each in-progress item has been in its current state",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamDetailPage = await overviewPage.goToTeam(team.name);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled({
			timeout: 90_000,
		});

		const metrics = await teamDetailPage.goToMetrics();
		const flowOverviewWidgets = await metrics.switchCategory(
			MetricsCategories.FlowOverview,
		);
		const workInProgressOverview = await metrics.getWidgetByName(
			"Work In Progress Overview",
			flowOverviewWidgets,
		);

		const workItemsDialog = await workInProgressOverview.openDialog();

		await expect(workItemsDialog.timeInStateColumnHeader).toBeVisible();

		const badges = await workItemsDialog.getTimeInStateBadges();
		expect(badges.length).toBeGreaterThan(0);
		for (const badge of badges) {
			expect(badge).toMatch(/\d+d in .+/);
		}

		await workItemsDialog.sortByTimeInState();
		await expect(workItemsDialog.timeInStateColumnHeader).toBeVisible();
	},
);
