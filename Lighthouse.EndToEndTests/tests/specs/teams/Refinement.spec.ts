import {
	expect,
	test,
	testWithDemoData,
} from "../../fixutres/LighthouseFixture";

// Premium scenario: it is the one that seeds Team Gravity, and Team Zenith beside it. CI uploads a
// licence to the instance before Playwright runs.
const DEPENDENCIES_SCENARIO_ID = 12;
const testWithDemo = testWithDemoData(DEPENDENCIES_SCENARIO_ID);

const CONFIGURED_TEAM = "Team Gravity";
const UNCONFIGURED_TEAM = "Team Zenith";

// Gravity's tracker ranks its Work Items in the order its demo file lists them, and the first one
// sitting in Backlog, Analysing or Next is GR-051.
const FIRST_IN_BACKLOG_ORDER = "GR-051";

// @walking_skeleton @driving_port @us-01 @us-02 @slice-01 @slice-02 @contract-shape:bounded-change
// Pending until DELIVER: the demo data names Gravity's refinement states (Backlog, Analysing, Next)
// and leaves Zenith without any (slice 01), and the tab lists what is in them (slice 02). Without
// sign-in everybody may edit the Team, so the switched-off tab points to Settings.
testWithDemo.fixme(
	"a coach opens a Team's Refinement tab and sees its Work Items in backlog order, while a Team without refinement states keeps the tab switched off",
	async ({ overviewPage, page }) => {
		await test.step("Gravity's Refinement tab lists what is in refinement, in backlog order", async () => {
			const gravity = await overviewPage.goToTeam(CONFIGURED_TEAM);

			await expect(gravity.refinementTab).toBeEnabled();
			const refinement = await gravity.goToRefinement();

			await expect(refinement.heading).toBeVisible();
			await expect(refinement.firstWorkItemRow).toContainText(
				FIRST_IN_BACKLOG_ORDER,
			);
			await expect(
				refinement.workItemRow(FIRST_IN_BACKLOG_ORDER),
			).toContainText("Analysing");
		});

		await test.step("Zenith's Refinement tab is switched off and says where to switch it on", async () => {
			await page.goto("/");
			const zenith = await overviewPage.goToTeam(UNCONFIGURED_TEAM);

			await expect(zenith.refinementTab).toBeDisabled();
			await expect(await zenith.refinementTabTooltip()).toHaveText(
				"Choose refinement states in Settings → Refinement",
			);
		});
	},
);
