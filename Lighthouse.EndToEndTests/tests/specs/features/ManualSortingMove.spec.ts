import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";
import { switchFeatureOrdering } from "../../helpers/api/optionalFeatures";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithFeatures = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

// One thin sanity check that the row action menu, the move endpoint, the rank service and the
// Features view are really wired to each other: a product owner sends the bottom row to the top and
// the list they are looking at reads it back.
//
// Moving needs this instance to own the order, a switch that outlives the spec. It starts from off
// and goes back to off afterwards, so the next spec meets the shipped default.
//
// Everything else sits a layer down and is covered there:
//   - insert-at-target, the non-contiguous Portfolio case, Move to Bottom past the unplaced tail, the
//     forecast moving, and every refusal -> Slice03RelativeMovesScenarios.cs
//   - the disabled states, the fail-open verdict trap, the four gestures' command shapes and the
//     keyboard path -> FeatureMoveMenu.test.tsx and useFeatureOrdering.moveGate.test.tsx
testWithFeatures.beforeEach(async ({ request }) => {
	await switchFeatureOrdering(request, false);
});

testWithFeatures.afterEach(async ({ request }) => {
	await switchFeatureOrdering(request, false);
});

testWithFeatures(
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
