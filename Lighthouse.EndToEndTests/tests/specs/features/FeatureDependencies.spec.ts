import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithFeatures = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

// The seeded Project Apollo already carries the shape this reads: "Mars Colonization" waits on the
// other two, and "Asteroid Mining Program" waits on nothing. All three are still open, so the list's
// "Hide Completed Features" default leaves them on screen.
//
// This used to name three real Epics on the letpeoplework board by id. They were released, which
// moved two of them to Closed and the third into a state this instance does not map at all, so the
// list came up empty and the build went red over a board someone tidied rather than over anything
// here.
const THE_DEPENDENT = "Mars Colonization";
const ONE_OF_ITS_BLOCKERS = "Stellar Navigation";
const THE_OTHER_BLOCKER = "Asteroid Mining Program";

// The walking skeleton for the whole slice: a dependency the work tracking system reports, the
// refresh that already runs, what that refresh stored, and a product owner reading the answer off a
// list in Lighthouse without opening the tracker.
//
// Everything else sits a layer down and is covered there:
//   - which links count, which are passed over, and what a removed link does -> the backend
//     dependency scenarios
//   - the blank cell, the heading's wording, the sort, and the link out to the tracker for a Feature
//     that has a url -> columns.dependsOn.test.tsx
testWithFeatures(
	"@walking_skeleton a product owner sees, without leaving Lighthouse, that a Feature is waiting on two others",
	async ({ testData, overviewPage }) => {
		expect(testData.portfolios.length).toBeGreaterThan(0);

		const featuresPage =
			await overviewPage.lighthousePage.goToFeatures("Features");
		await expect(featuresPage.featureRows.first()).toBeVisible();

		// The row names them rather than counting them: "which ones" is the question a reader has
		// next. Seeded Features have no address in a tracker to link to, so what is read here is the
		// naming.
		const dependencies = featuresPage.getDependenciesCell(THE_DEPENDENT);
		await expect(dependencies).toContainText(ONE_OF_ITS_BLOCKERS);
		await expect(dependencies).toContainText(THE_OTHER_BLOCKER);

		await expect(
			featuresPage.getDependenciesCell(THE_OTHER_BLOCKER),
		).toHaveText("");
	},
);
