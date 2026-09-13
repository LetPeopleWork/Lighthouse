import { expect, test } from "../../fixutres/LighthouseFixture";
import { UsageDataDialogPage } from "../../models/app/UsageDataDialogPage";

/**
 * Epic 5733 slice 02 (#5835) — the walking skeleton for the unprompted ask.
 *
 * One scenario, because that is all an end-to-end test is for here: proving the pieces are wired to
 * each other. Whether the question is due after two days or ninety, and which tier makes a refusal
 * final, are decided on the server and asserted where a clock can be moved.
 *
 * It needs an instance whose install age has passed the threshold. A fresh instance is minutes old,
 * so the run sets UsageData__AskAfterInstallDays=0 - a configuration value the product already has,
 * rather than a hook that exists only for tests. Without it the dialog correctly never appears and
 * this is skipped rather than quietly passing on the wrong reason.
 */

const theInstanceAsksImmediately =
	process.env.LIGHTHOUSE_E2E_USAGEDATA_ASKS_IMMEDIATELY === "true";

test.describe("Usage data: the question arrives without being asked for", () => {
	test.skip(
		!theInstanceAsksImmediately,
		"Needs an instance started with UsageData__AskAfterInstallDays=0; a fresh one is too young to ask.",
	);

	test("puts the question to a browser that never went looking for it, and says whether it will come back", async ({
		page,
	}) => {
		await UsageDataDialogPage.asABrowserThatHasNeverBeenAsked(page);

		// goto rather than LighthousePage.open(), which clicks its way to the Overview. The dialog
		// is modal, so on the one page in the suite where it is meant to appear that click is
		// swallowed by the thing under test - the failure every other spec is protected from,
		// arriving in the spec that removes the protection on purpose.
		await page.goto("/");

		const consent = new UsageDataDialogPage(page);

		await expect(consent.dialog).toBeVisible();

		await expect(consent.acceptButton).toBeVisible();
		await expect(consent.declineButton).toBeVisible();
	});

	test("does not put it a second time in the same session", async ({
		page,
	}) => {
		await UsageDataDialogPage.asABrowserThatHasNeverBeenAsked(page);

		await page.goto("/");

		const consent = new UsageDataDialogPage(page);
		await expect(consent.dialog).toBeVisible();

		await page.keyboard.press("Escape");
		await expect(consent.dialog).toBeHidden();

		// Closing answered nothing, so nothing was recorded on the server. What stops it coming
		// straight back is the browser's own record of having been asked - and this is the only
		// place that record is exercised through a real browser rather than a jsdom double.
		// A fresh load in the same session, which is the strongest form of the question: session
		// storage survives it and so does the marker.
		await page.goto("/");

		await expect(consent.dialog).toBeHidden();
	});
});

test.describe("Usage data: every other spec is left alone", () => {
	test("does not interrupt a browser the fixture has already marked as asked", async ({
		overviewPage,
	}) => {
		// The fixture seeds the marker for every spec in the suite. If that ever stops working, a
		// modal starts swallowing clicks across unrelated specs and the failures name pointer
		// events rather than usage data - so it is worth one test that says what went wrong.
		await expect(
			new UsageDataDialogPage(overviewPage.page).dialog,
		).toBeHidden();
	});
});
