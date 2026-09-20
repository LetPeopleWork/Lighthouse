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

// Epic 6033 slice 02 walking skeleton — US-02. One thin sanity check that a start date survives the
// whole way: the simulation records the day it first pulls each Feature, the read serves it, and the
// table draws it in a column of its own against a seeded instance.
//
// Everything else sits a layer down and is covered there:
//   - the three sources, the observed marking and the reused empty state -> columns.forecastedStart.test.tsx
//   - the day the run records, at both grains -> ForecastServiceStartDayTest.cs
//   - the payload the column reads -> the ForecastedStartDates acceptance scenarios
testWithFeatures(
	"should say when work on each feature is expected to begin",
	// testData is destructured even though the assertions below never read it. Playwright builds only
	// the fixtures a test asks for, and that one is what seeds the demo scenario - leave it out and the
	// instance is empty, which reads as "the column is missing" rather than "there is nothing to show".
	async ({ testData, overviewPage }) => {
		expect(testData.portfolios.length).toBeGreaterThan(0);

		const featuresPage =
			await overviewPage.lighthousePage.goToFeatures("Features");

		await expect(featuresPage.featureRows.first()).toBeVisible();

		const forecastedStarts = await featuresPage.getListedForecastedStarts();

		expect(forecastedStarts.length).toBeGreaterThan(0);

		// At least one seeded Feature has an answer. Not every one will: the demo data includes work
		// nobody can forecast, and a blank cell there is the column behaving correctly rather than the
		// column being absent.
		expect(forecastedStarts.some((cell) => cell.length > 0)).toBe(true);
	},
);
