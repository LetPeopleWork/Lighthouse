import { expect, test } from "../../fixutres/LighthouseFixture";
import { waitForBackgroundUpdates } from "../../helpers/api/demo";
import { createPortfolio } from "../../helpers/api/portfolios";
import { createTeam } from "../../helpers/api/teams";
import { createAzureDevOpsConnection } from "../../helpers/api/workTrackingSystemConnections";
import { generateRandomName } from "../../helpers/names";

const adoStates = {
	toDo: ["New", "Planned"],
	doing: ["Active", "Resolved"],
	done: ["Closed"],
};

// Three real Epics on the letpeoplework board, linked to each other with the tracker's own Predecessor
// links: "Dependency-Aware Forecasting" waits on the other two. Naming them one by one, instead of
// taking the whole project, is what keeps the number on screen a fact rather than a reading of
// today's board - a Predecessor link pointing outside this set is passed over, so someone adding a
// dependency tomorrow cannot make this fail somewhere far from what they changed.
const THE_DEPENDENT_AND_BOTH_OF_ITS_BLOCKERS =
	'[System.TeamProject] = "Lighthouse" AND [System.Id] IN (4365, 5698, 5792)';

const THE_DEPENDENT = "Dependency-Aware Forecasting";
const ONE_OF_ITS_BLOCKERS = "Show Feature Dependencies";
const THE_OTHER_BLOCKER = "Deliveries as Durable Records";

// The walking skeleton for the whole slice: a Predecessor link a person drew in Azure DevOps, the
// refresh that already runs, what that refresh stored, and a product owner reading the answer off a
// list in Lighthouse without opening the tracker.
//
// Everything else sits a layer down and is covered there:
//   - which links count, which are passed over, and what a removed link does -> the backend
//     dependency scenarios
//   - the blank cell, the heading's wording and the sort -> columns.dependsOn.test.tsx
test("@walking_skeleton a product owner sees, without leaving Lighthouse, that a Feature is waiting on two others", async ({
	request,
	overviewPage,
}) => {
	const connection = await createAzureDevOpsConnection(
		request,
		generateRandomName(),
	);
	// Lighthouse refuses to create a Portfolio while the instance has no Team at all, so there has to
	// be one. It is never refreshed: nothing this test reads comes from below the Epics.
	await createTeam(
		request,
		generateRandomName(),
		connection.id,
		'[System.TeamProject] = "Lighthouse"',
		["User Story"],
		adoStates,
	);

	const portfolio = await createPortfolio(
		request,
		generateRandomName(),
		connection.id,
		THE_DEPENDENT_AND_BOTH_OF_ITS_BLOCKERS,
		["Epic"],
		adoStates,
	);

	const lighthousePage = overviewPage.lighthousePage;
	const portfolioPage = await (
		await lighthousePage.goToOverview()
	).goToPortfolio(portfolio.name);
	await portfolioPage.refreshFeatures();
	await waitForBackgroundUpdates(request);

	const featuresPage = await lighthousePage.goToFeatures("Features");
	await expect(featuresPage.featureRows.first()).toBeVisible();

	// The row names them rather than counting them: "which ones" is the question a reader has next,
	// and each answer leads into the work tracking system where something can be done about it.
	const dependencies = featuresPage.getDependenciesCell(THE_DEPENDENT);
	await expect(
		dependencies.getByRole("link", { name: new RegExp(ONE_OF_ITS_BLOCKERS) }),
	).toBeVisible();
	await expect(
		dependencies.getByRole("link", { name: new RegExp(THE_OTHER_BLOCKER) }),
	).toBeVisible();

	await expect(
		featuresPage.getDependenciesCell(ONE_OF_ITS_BLOCKERS),
	).toHaveText("");
});
