import { expect, test } from "../../fixutres/LighthouseFixture";

/**
 * The one browser flow for lists longer than a page. Settings → System → Third Party Packages lists
 * both bills of materials and needs no demo data. Builds of main carry the real bills, a couple of
 * hundred packages; every other build, a pull request's included, carries one-package placeholders.
 * So the spec counts what the build serves first and takes one of two branches: past a page it pages
 * through the list and searches it, otherwise it checks the list is short and whole and stops. In
 * pull-request CI that means only the short look is proven here; the paging is proven by the
 * component tests, and in a browser by CI on main. Where the footer starts, the boundaries and how a
 * page reacts to sorting, filtering and reloads are pinned in the component tests.
 */

const PAGE_SIZE = 100;

test("A long list shows its range, pages to the rest, and a search opens its first page", async ({
	overviewPage,
}) => {
	const settingsPage = await overviewPage.lightHousePage.goToSettings();
	const systemInfo = await settingsPage.goToSystemInfo();
	await systemInfo.openThirdPartyPackages();

	const served = await systemInfo.servedPackageCount();
	expect(await systemInfo.packagesGridRowCount()).toBe(served);

	if (served <= PAGE_SIZE) {
		await expect(systemInfo.packageRows).toHaveCount(served);
		await expect(systemInfo.packagesRowRange).toHaveCount(0);
		await expect(systemInfo.nextPackagesPageButton).toHaveCount(0);
		test.info().annotations.push({
			type: "short list",
			description: `This build serves ${served} packages, too few to page; it carries the placeholder bills of materials.`,
		});
		return;
	}

	await expect(systemInfo.packagesRowRange).toHaveText(`1–100 of ${served}`);
	await expect(systemInfo.packagesRowsPerPageChoice).toHaveCount(0);
	const firstOnPageOne = await systemInfo.firstPackageName.textContent();
	if (!firstOnPageOne) {
		throw new Error("The first package on page 1 has no name to compare.");
	}

	await systemInfo.goToNextPackagesPage();

	await expect(systemInfo.packagesRowRange).toHaveText(/^101–\d+ of \d+$/);
	await expect(systemInfo.firstPackageName).not.toHaveText(firstOnPageOne);

	// Most packages in either bill of materials have an "e" in their name, so the search leaves more
	// than a page and has to open its first one. Searching main's bills for "e" left 203 of 233.
	await systemInfo.searchPackages("e");

	await expect(systemInfo.packagesRowRange).toHaveText(/^1–100 of \d+$/);
	const found = await systemInfo.packagesRangeTotal();
	expect(found).toBeGreaterThan(PAGE_SIZE);
	expect(found).toBeLessThan(served);
});
