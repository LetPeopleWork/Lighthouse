import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithFeatures = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

// Epic 5375 slice 01 walking skeleton — US-01. One thin sanity check that the nav entry, the route,
// the read endpoint and the position column are really wired together against a seeded instance.
//
// Everything else sits a layer down and is covered there:
//   - the RBAC result set, the global position, the shared Feature, the unranked Feature, the
//     unlicensed instance and the 500-row read -> Slice01FeaturesViewScenarios.cs
//   - the position cell's rendering and the terminology-driven nav label -> columns.position.test.tsx
//     and Header.featuresNav.test.tsx
testWithFeatures(
	"should list the seeded features in forecast order, each showing where it sits",
	async ({ testData, overviewPage }) => {
		expect(testData.portfolios.length).toBeGreaterThan(0);

		const featuresPage =
			await overviewPage.lighthousePage.goToFeatures("Features");

		await expect(featuresPage.featureRows.first()).toBeVisible();
		await expect(featuresPage.helpText).toBeVisible();

		const positions = await featuresPage.getListedPositions();

		expect(positions.length).toBeGreaterThan(0);
		expect(positions.every((position) => Number.isInteger(position))).toBe(
			true,
		);
		expect([...positions]).toEqual([...positions].sort((a, b) => a - b));
	},
);
