import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithFeatures = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

// Epic 5375 slice 02 walking skeleton — US-02. One thin sanity check that the switch on Settings →
// System, the ordering-policy endpoint, the seed and the Features view are really wired to each other:
// a config admin hands the order over and the list they were looking at does not move.
//
// Everything else sits a layer down and is covered there:
//   - the five connector Order shapes, the five refreshes, the two ways in agreeing, giving the order
//     back and taking it over again, the licence and the role -> Slice02ManualRankScenarios.cs
//   - the switch, its premium affordance, its help text and the column's heading ->
//     FeatureOrderingSettings.test.tsx and useFeatureOrdering.test.tsx
testWithFeatures.skip(
	"@premium @walking_skeleton a config admin hands the order to this instance and nothing moves",
	async ({ testData, overviewPage }) => {
		expect(testData.portfolios.length).toBeGreaterThan(0);

		const lighthousePage = overviewPage.lighthousePage;

		const featuresPage = await lighthousePage.goToFeatures("Features");
		await expect(featuresPage.featureRows.first()).toBeVisible();
		const beforeTheSwitch = await featuresPage.getListedFeatureNames();

		const settingsPage = await lighthousePage.goToSettings();
		const systemConfiguration = await settingsPage.goToSystemConfiguration();
		await systemConfiguration.handOrderingOverToThisInstance();

		const featuresPageAgain = await lighthousePage.goToFeatures("Features");
		await expect(featuresPageAgain.featureRows.first()).toBeVisible();

		expect(await featuresPageAgain.getListedFeatureNames()).toEqual(
			beforeTheSwitch,
		);
		expect(await featuresPageAgain.getPositionColumnHeading()).toBe("Manual");
	},
);
