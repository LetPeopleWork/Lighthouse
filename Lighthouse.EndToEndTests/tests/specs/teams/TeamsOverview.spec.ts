import {
	expect,
	test,
	testWithDemoData,
} from "../../fixutres/LighthouseFixture";

const PRODUCT_LAUNCH_SCENARIO_ID = 2;
const testWithTeams = testWithDemoData(PRODUCT_LAUNCH_SCENARIO_ID);

testWithTeams(
	"should show all teams on Teams Overview",
	async ({ testData, overviewPage }) => {
		expect(testData.teams.length).toBeGreaterThan(1);

		for (const team of testData.teams) {
			const teamLink = await overviewPage.getTeamLink(team.name);
			await expect(teamLink).toBeVisible();
		}
	},
);

testWithTeams(
	"should filter teams on Teams Overview",
	async ({ testData, overviewPage }) => {
		const [team1, team2] = testData.teams;

		await test.step(`Search for Team ${team1.name}`, async () => {
			await overviewPage.search(team1.name);

			const teamLink1 = await overviewPage.getTeamLink(team1.name);
			const teamLink2 = await overviewPage.getTeamLink(team2.name);

			await expect(teamLink1).toBeVisible();
			await expect(teamLink2).not.toBeVisible();
		});

		await test.step(`Search for Team ${team2.name}`, async () => {
			await overviewPage.search(team2.name);

			const teamLink1 = await overviewPage.getTeamLink(team1.name);
			const teamLink2 = await overviewPage.getTeamLink(team2.name);

			await expect(teamLink1).not.toBeVisible();
			await expect(teamLink2).toBeVisible();
		});

		await test.step("Search for not existing Team", async () => {
			await overviewPage.search("Jambalaya");

			const teamLink1 = await overviewPage.getTeamLink(team1.name);
			const teamLink2 = await overviewPage.getTeamLink(team2.name);

			await expect(teamLink1).not.toBeVisible();
			await expect(teamLink2).not.toBeVisible();
		});
	},
);

testWithTeams(
	"should open Team Info when clicking on Team",
	async ({ testData, overviewPage }) => {
		const [team1] = testData.teams;

		const teamDetailPage = await overviewPage.goToTeam(team1.name);
		expect(teamDetailPage.page.url()).toContain(`/teams/${team1.id}`);
	},
);

testWithTeams(
	"should open Team Edit Page when clicking on Edit Icon",
	async ({ testData, overviewPage }) => {
		const [team1] = testData.teams;

		const teamDetailPage = await overviewPage.editTeam(team1.name);
		expect(teamDetailPage.page.url()).toContain(`/teams/${team1.id}/settings`);
	},
);

testWithTeams(
	"should delete Team when clicking on Delete Icon and confirming",
	async ({ testData, overviewPage }) => {
		const [team1] = testData.teams;

		await test.step(`Delete Team ${team1.name}`, async () => {
			const teamDeletionDialog = await overviewPage.deleteTeam(team1.name);
			await teamDeletionDialog.delete();
		});

		await test.step(`Search for Team ${team1.name}`, async () => {
			await overviewPage.search(team1.name);
			const teamLink = await overviewPage.getTeamLink(team1.name);

			await expect(teamLink).not.toBeVisible();
		});
	},
);

testWithTeams(
	"should not delete Team when clicking on Delete Icon and cancelling",
	async ({ testData, overviewPage }) => {
		const [team1] = testData.teams;

		await test.step(`Delete Team ${team1.name}`, async () => {
			const teamDeletionDialog = await overviewPage.deleteTeam(team1.name);
			await teamDeletionDialog.cancel();
		});

		await test.step(`Search for Team ${team1.name}`, async () => {
			await overviewPage.search(team1.name);

			const teamLink = await overviewPage.getTeamLink(team1.name);
			await expect(teamLink).toBeVisible();
		});
	},
);

testWithTeams(
	"should clone team when clicking on Clone icon",
	async ({ testData, overviewPage }) => {
		const team1 = testData.teams[0];

		await test.step(`Clone Team ${team1.name}`, async () => {
			const teamEditPage = await overviewPage.cloneTeam(team1.name);

			expect(teamEditPage.page.url()).toContain("/teams/new");
			expect(teamEditPage.page.url()).toContain(`cloneFrom=${team1.id}`);

			const nameField = await teamEditPage.getName();
			expect(nameField).toBe(`Copy of ${team1.name}`);
		});
	},
);
