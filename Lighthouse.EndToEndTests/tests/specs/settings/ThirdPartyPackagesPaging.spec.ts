import { expect, test } from "../../fixutres/LighthouseFixture";

/**
 * The one browser flow for lists longer than a page. Settings → System → Third Party Packages lists
 * both bills of materials, well over 100 packages in any real build, so it is the one list every
 * instance has that runs past a page and needs no demo data. Where the footer starts, the boundaries
 * and how a page reacts to sorting, filtering and reloads are pinned in the component tests.
 *
 * Skipped until the paging footer is built.
 */

test.skip("A long list shows its range, pages to the rest, and a search opens its first page @walking_skeleton @driving_port @us-01 @kpi-OUT-6202-no-hidden-rows @contract-shape:bounded-change", async ({
	overviewPage,
}) => {
	const settingsPage = await overviewPage.lightHousePage.goToSettings();
	const systemInfo = await settingsPage.goToSystemInfo();
	await systemInfo.openThirdPartyPackages();

	await expect(systemInfo.packagesRowRange).toHaveText(/^1–100 of \d+$/);
	await expect(systemInfo.packagesRowsPerPageChoice).toHaveCount(0);
	const firstOnPageOne = await systemInfo.firstPackageName.textContent();
	if (!firstOnPageOne) {
		throw new Error("The first package on page 1 has no name to compare.");
	}

	await systemInfo.goToNextPackagesPage();

	await expect(systemInfo.packagesRowRange).toHaveText(/^101–\d+ of \d+$/);
	await expect(systemInfo.firstPackageName).not.toHaveText(firstOnPageOne);

	// Most package names in either bill of materials contain an "e", so the search leaves more than
	// a page and has to open its first one. DELIVER confirms the count on its first live run.
	await systemInfo.searchPackages("e");

	await expect(systemInfo.packagesRowRange).toHaveText(/^1–100 of \d+$/);
});
