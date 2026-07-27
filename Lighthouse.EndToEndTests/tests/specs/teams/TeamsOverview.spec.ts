import {
	expect,
	test,
	testWithDemoData,
} from "../../fixutres/LighthouseFixture";

const PRODUCT_LAUNCH_SCENARIO_ID = 2;
const testWithTeams = testWithDemoData(PRODUCT_LAUNCH_SCENARIO_ID);

// Seven specs used to walk this one grid — list, filter, open, edit, clone, delete,
// cancel-delete — re-seeding the Product Launch scenario for each. They are two
// visits now: one read-only walk and one destructive walk, because a confirmed
// delete is the only step that cannot share a fixture with the others.
//
// The grid itself (filtering by name and tag, alphabetical order, the clone URL, the
// per-row Edit/Clone/Delete predicates, the empty-filter message) is covered
// component-side in DataOverviewTable.test.tsx, and the delete dialog in
// DeleteConfirmationDialog.test.tsx. What is proven here is that the real page wires
// them to real teams.
testWithTeams(
	"should list, filter, open, edit, and clone teams from the Teams Overview",
	async ({ testData, overviewPage }) => {
		expect(testData.teams.length).toBeGreaterThan(1);

		const [team1, team2] = testData.teams;

		await test.step("All seeded teams are listed", async () => {
			for (const team of testData.teams) {
				const teamLink = await overviewPage.getTeamLink(team.name);
				await expect(teamLink).toBeVisible();
			}
		});

		await test.step(`Search narrows to Team ${team1.name}`, async () => {
			await overviewPage.search(team1.name);

			await expect(await overviewPage.getTeamLink(team1.name)).toBeVisible();
			await expect(
				await overviewPage.getTeamLink(team2.name),
			).not.toBeVisible();
		});

		await test.step(`Search narrows to Team ${team2.name}`, async () => {
			await overviewPage.search(team2.name);

			await expect(
				await overviewPage.getTeamLink(team1.name),
			).not.toBeVisible();
			await expect(await overviewPage.getTeamLink(team2.name)).toBeVisible();
		});

		await test.step("Search for a team that does not exist shows nothing", async () => {
			await overviewPage.search("Jambalaya");

			await expect(
				await overviewPage.getTeamLink(team1.name),
			).not.toBeVisible();
			await expect(
				await overviewPage.getTeamLink(team2.name),
			).not.toBeVisible();
		});

		await test.step("Clicking a team opens its detail page", async () => {
			const teamsPage = await overviewPage.lightHousePage.goToOverview();
			const teamDetailPage = await teamsPage.goToTeam(team1.name);
			expect(teamDetailPage.page.url()).toContain(`/teams/${team1.id}`);
		});

		await test.step("The Edit icon opens the team settings page", async () => {
			const teamsPage = await overviewPage.lightHousePage.goToOverview();
			const teamEditPage = await teamsPage.editTeam(team1.name);
			expect(teamEditPage.page.url()).toContain(`/teams/${team1.id}/settings`);
		});

		await test.step("The Clone icon opens a pre-filled new-team page", async () => {
			const teamsPage = await overviewPage.lightHousePage.goToOverview();
			const teamEditPage = await teamsPage.cloneTeam(team1.name);

			expect(teamEditPage.page.url()).toContain("/teams/new");
			expect(teamEditPage.page.url()).toContain(`cloneFrom=${team1.id}`);

			const nameField = await teamEditPage.getName();
			expect(nameField).toBe(`Copy of ${team1.name}`);
		});
	},
);

testWithTeams(
	"should keep the team when a deletion is cancelled and remove it when confirmed",
	async ({ testData, overviewPage }) => {
		const [team1] = testData.teams;

		await test.step(`Cancelling the deletion keeps Team ${team1.name}`, async () => {
			const teamDeletionDialog = await overviewPage.deleteTeam(team1.name);
			await teamDeletionDialog.cancel();

			await overviewPage.search(team1.name);
			await expect(await overviewPage.getTeamLink(team1.name)).toBeVisible();
		});

		await test.step(`Confirming the deletion removes Team ${team1.name}`, async () => {
			const teamsPage = await overviewPage.lightHousePage.goToOverview();
			const teamDeletionDialog = await teamsPage.deleteTeam(team1.name);
			await teamDeletionDialog.delete();

			await teamsPage.search(team1.name);
			await expect(await teamsPage.getTeamLink(team1.name)).not.toBeVisible();
		});
	},
);
