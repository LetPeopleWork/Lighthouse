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
	"a product owner sees, without leaving Lighthouse, that a Feature is waiting on two others",
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

// A narrow window leaves the Dependencies column narrower than the names it lists. Each dependency
// keeps a line of its own, cut with an ellipsis inside the cell, and the hover names all of them.
testWithFeatures(
	"a product owner on a narrow window sees each dependency cut on its own line and reads them all in full on hover",
	async ({ testData, overviewPage }) => {
		expect(testData.portfolios.length).toBeGreaterThan(0);
		const featuresPage =
			await overviewPage.lighthousePage.goToFeatures("Features");
		await expect(featuresPage.featureRows.first()).toBeVisible();
		await overviewPage.lighthousePage.page.setViewportSize({
			width: 800,
			height: 900,
		});
		await expect(
			featuresPage.getDependenciesCell(THE_DEPENDENT),
			"the Features list must have drawn the dependent's Dependencies cell",
		).toBeVisible();

		const lines = featuresPage.dependencyLinesOf(THE_DEPENDENT);
		await expect(lines, "one line per dependency").toHaveCount(2);
		for (const index of [0, 1]) {
			await expect
				.poll(() => featuresPage.describeDependencyLine(THE_DEPENDENT, index), {
					message: `dependency ${index + 1} must be cut to its column with an ellipsis, on one line, inside the cell`,
				})
				.toEqual({
					isCut: true,
					endsWithEllipsis: true,
					isOneLine: true,
					endsInsideCell: true,
				});
		}

		await featuresPage.hoverDependenciesOf(THE_DEPENDENT);
		await expect(
			featuresPage.tooltipNaming(ONE_OF_ITS_BLOCKERS),
			"hovering the cut list must name every dependency",
		).toBeVisible();
		await expect(featuresPage.tooltipNaming(THE_OTHER_BLOCKER)).toBeVisible();
	},
);
