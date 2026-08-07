import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithFeatures = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

// Epic 5375 slice 03 walking skeleton — US-03. One thin sanity check that the row action menu, the move
// endpoint, the rank service and the Features view are really wired to each other: a product owner
// sends the bottom row to the top and the list they are looking at reads it back.
//
// Everything else sits a layer down and is covered there:
//   - insert-at-target, the non-contiguous Portfolio case, Move to Bottom past the unplaced tail, the
//     forecast moving, and every refusal -> Slice03RelativeMovesScenarios.cs
//   - the disabled states, the fail-open verdict trap, the four gestures' command shapes and the
//     keyboard path -> FeatureMoveMenu.test.tsx and useFeatureOrdering.moveGate.test.tsx
testWithFeatures.skip(
	"@premium @walking_skeleton a product owner sends a Feature to the top and the order reads it back",
	async ({ testData, overviewPage }) => {
		expect(testData.portfolios.length).toBeGreaterThan(0);

		const lighthousePage = overviewPage.lighthousePage;

		const settingsPage = await lighthousePage.goToSettings();
		const systemConfiguration = await settingsPage.goToSystemConfiguration();
		await systemConfiguration.handOrderingOverToThisInstance();

		const featuresPage = await lighthousePage.goToFeatures("Features");
		await expect(featuresPage.featureRows.first()).toBeVisible();

		const beforeTheMove = await featuresPage.getListedFeatureNames();
		expect(beforeTheMove.length).toBeGreaterThan(1);
		const theOneNobodyGetsTo = beforeTheMove[beforeTheMove.length - 1];

		await featuresPage.moveToTop(theOneNobodyGetsTo);

		const featuresPageAgain = await lighthousePage.goToFeatures("Features");
		await expect(featuresPageAgain.featureRows.first()).toBeVisible();

		const afterTheMove = await featuresPageAgain.getListedFeatureNames();
		expect(afterTheMove[0]).toBe(theOneNobodyGetsTo);
		expect(afterTheMove).toHaveLength(beforeTheMove.length);
	},
);
