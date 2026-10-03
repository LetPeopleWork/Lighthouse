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
// The demo data names Gravity's refinement states (Backlog, Analysing, Next) and leaves Zenith
// without any, and the tab lists what is in them. Without sign-in everybody may edit the Team, so the
// switched-off tab points to Settings.
testWithDemo(
	"a coach opens a Team's Refinement tab and sees its Work Items in backlog order, while a Team without refinement states keeps the tab switched off",
	async ({ testData, overviewPage, page }) => {
		expect(testData.teams.map((team) => team.name)).toEqual(
			expect.arrayContaining([CONFIGURED_TEAM, UNCONFIGURED_TEAM]),
		);

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

// The demo data gives Gravity's GR-059 two Yes votes, from Jonas Weber and Mo Okafor, so one more makes
// it Ready at the default of three Yes votes from three voters.
const TWO_YES_SHORT_OF_READY = "GR-059";

// @walking_skeleton @driving_port @us-11 @us-13 @slice-11 @slice-13 @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:bounded-change
// Without sign-in a voter gives a name once, votes from the list, sees the vote counted, and the third Yes
// makes the Work Item Ready for everybody.
testWithDemo.fixme(
	"a voter gives a name, says Yes on a Work Item in refinement and the votes make it Ready",
	async ({ overviewPage }) => {
		const gravity = await overviewPage.goToTeam(CONFIGURED_TEAM);
		const refinement = await gravity.goToRefinement();

		await test.step("the Work Item still needs one more Yes", async () => {
			await expect(
				refinement.workItemRow(TWO_YES_SHORT_OF_READY),
			).toContainText("1 more Yes needed");
		});

		await test.step("Priya says Yes, giving her name the first time", async () => {
			await refinement.vote(TWO_YES_SHORT_OF_READY, "Yes", "Priya Sharma");

			await expect(
				refinement.answerButton(TWO_YES_SHORT_OF_READY, "Yes"),
			).toHaveAttribute("aria-pressed", "true");
			await expect(
				refinement.workItemRow(TWO_YES_SHORT_OF_READY),
			).toContainText("3 votes");
		});

		await test.step("three Yes votes make it Ready", async () => {
			await expect(
				refinement.workItemRow(TWO_YES_SHORT_OF_READY),
			).toContainText("Ready");
		});
	},
);
